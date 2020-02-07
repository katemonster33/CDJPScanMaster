/*
 * J1850VPW.c
 *
 * Created: 2/4/2018 2:03:06 AM
 *  Author: Katie M
 */
 #include <asf.h>
 #include "main.h"
 #include "string.h"
 
 // receiving pulse width
 #define MICROSEC_TO_CNT_SCALE 1

 uint16_t RX_SHORT_MIN	        = 16 * MICROSEC_TO_CNT_SCALE;	// minimum short pulse time
 uint16_t RX_LONG_MIN          = 96 * MICROSEC_TO_CNT_SCALE;	// minimum long pulse time
 uint16_t RX_SOF_MIN		        = 163 * MICROSEC_TO_CNT_SCALE;	// minimum start of frame time
 uint16_t RX_SOF_MAX		        = 239 * MICROSEC_TO_CNT_SCALE;	// maximum start of frame time
 uint16_t RX_EOF_MIN		        = 239 * MICROSEC_TO_CNT_SCALE;	// minimum end of frame time, ends at minimum IFS
 uint16_t RX_BRK_MIN		        = 239 * MICROSEC_TO_CNT_SCALE;	// minimum break time
 uint16_t RX_IFS_MIN		        = 280 * MICROSEC_TO_CNT_SCALE;	// minimum inter frame separation time, ends at next SOF

 uint16_t TX_SHORT             = 64 * MICROSEC_TO_CNT_SCALE;   // Short pulse nominal time
 uint16_t TX_LONG              = 128 * MICROSEC_TO_CNT_SCALE;    // Long pulse nominal time
 uint16_t TX_SOF               = 200 * MICROSEC_TO_CNT_SCALE;    // Start Of Frame nominal time
 uint16_t TX_EOD               = 200 * MICROSEC_TO_CNT_SCALE;    // End Of Data nominal time
 uint16_t TX_EOF               = 280 * MICROSEC_TO_CNT_SCALE;    // End Of Frame nominal time
 uint16_t TX_BRK               = 300 * MICROSEC_TO_CNT_SCALE;    // Break nominal time
 uint16_t TX_IFS               = 280 * MICROSEC_TO_CNT_SCALE;    // Inter Frame Separation nominal time

#define PIN_J1850VPW_RX			PIN0_bm // PORT D
#define PIN_J1850VPW_TX			PIN1_bm // PORT D

#define TIMER_J1850VPW			TCD0

void j1850_rx_push_bit(bool highBit);
uint8_t j1850vpw_calc_crc(uint8_t *buffer, uint8_t len);

bool transmitActive = false;
uint8_t currentBit = 0;

uint8_t j1850_rx_buffer[64];
uint8_t j1850_rx_buffer_len = 0;
//struct byte_buffer j1850_tx_buffer;

bool lastJ1850State = false; // false = inactive, true = active
bool rxInProgress = false; // when a start-of-frame happens on the bus, we set this to true until end-of-frame occurs

void j1850vpw_setup(void)
{
	PORTD.DIRSET |= PIN_J1850VPW_TX;
}

void j1850vpw_do_tasks(struct byte_buffer *txBuffer)
{
	bool currentJ1850State = (PORTD.IN & PIN_J1850VPW_RX) != 0;
	if(rxInProgress == false)
	{
		// if J1850 just went high, then we're waiting for timer to reach RX_SOF_MIN
		if(currentJ1850State == true && lastJ1850State == false) TIMER_J1850VPW.CNT = 0;	
		else if(currentJ1850State == false && lastJ1850State == true && TIMER_J1850VPW.CNT > RX_SOF_MIN) // have we reached RX_SOF_MIN?
		{
			rxInProgress = true;
		}
		if(txBuffer)
		{
			//if()
		}
	}
	else
	{
		if(currentJ1850State != lastJ1850State)
		{
			// is this a short pulse?
			if(TIMER_J1850VPW.CNT >= RX_SHORT_MIN && TIMER_J1850VPW.CNT < RX_LONG_MIN)
			{
				j1850_rx_push_bit(lastJ1850State); 
			}
			// long pulse ?
			else if(TIMER_J1850VPW.CNT < RX_EOF_MIN)
			{
				j1850_rx_push_bit(!lastJ1850State);
			}
			TIMER_J1850VPW.CNT = 0;
		}
		else if(TIMER_J1850VPW.CNT > RX_EOF_MIN)
		{
			rxInProgress = false;
			uint8_t crc = j1850vpw_calc_crc(j1850_rx_buffer, j1850_rx_buffer_len - 1);
			if(crc == j1850_rx_buffer[j1850_rx_buffer_len - 1])
			{
				j1850_rx_buffer_len--;
				bt_queue_rx(j1850_rx_buffer, j1850_rx_buffer_len, PAYLOAD_PROTOCOL_J1850);
			}
			j1850_rx_buffer_len = 0;
		}
	}

	lastJ1850State = currentJ1850State;
	
}

void j1850_rx_push_bit(bool highBit)
{
	if(highBit) j1850_rx_buffer[j1850_rx_buffer_len] |= (1 << currentBit);
	if(currentBit < 7) currentBit++;
	else
	{
		currentBit = 0;
		j1850_rx_buffer_len++;
	}
}

uint8_t j1850vpw_calc_crc(uint8_t *buffer, uint8_t len)
{
	uint8_t crc_reg=0xff,poly,i,j;
	uint8_t *byte_point;
	uint8_t bit_point;

	for (i = 0, byte_point = buffer; i < len; ++i, ++byte_point)
	{
		for (j = 0, bit_point = 0x80 ; j < 8; ++j, bit_point >>= 1)
		{
			if (bit_point & *byte_point)	// case for new bit = 1
			{
				poly = (crc_reg & 0x80 ? 1 : 0x1C);
				crc_reg= ( (crc_reg << 1) | 1) ^ poly;
			}
			else		// case for new bit = 0
			{
				poly = (crc_reg & 0x80 ? 0x1D : 0);
				crc_reg = (crc_reg << 1) ^ poly;
			}
		}
	}
	return ~crc_reg;	// Return CRC
}
