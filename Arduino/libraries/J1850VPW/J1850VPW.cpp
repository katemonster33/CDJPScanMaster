#include "J1850VPW.h"

// receiving pulse width
#define US_TO_CNT_SCALE 1
#define US_TO_CNT_SCALE_TX 4

uint16_t RX_SHORT_MIN	        = 16 * US_TO_CNT_SCALE;	// minimum short pulse time
uint16_t RX_SHORT_MAX         = 96 * US_TO_CNT_SCALE;	// maximum short pulse time
uint16_t RX_LONG_MIN          = 96 * US_TO_CNT_SCALE;	// minimum long pulse time
uint16_t RX_LONG_MAX		      = 163 * US_TO_CNT_SCALE;	// maximum long pulse time
uint16_t RX_SOF_MIN		        = 163 * US_TO_CNT_SCALE;	// minimum start of frame time
uint16_t RX_SOF_MAX		        = 239 * US_TO_CNT_SCALE;	// maximum start of frame time
uint16_t RX_EOD_MIN		        = 163 * US_TO_CNT_SCALE;	// minimum end of data time
uint16_t RX_EOD_MAX		        = 239 * US_TO_CNT_SCALE;	// maximum end of data time
uint16_t RX_EOF_MIN		        = 239 * US_TO_CNT_SCALE;	// minimum end of frame time, ends at minimum IFS
uint16_t RX_BRK_MIN		        = 239 * US_TO_CNT_SCALE;	// minimum break time
uint16_t RX_IFS_MIN		        = 280 * US_TO_CNT_SCALE;	// minimum inter frame separation time, ends at next SOF

uint8_t TX_SHORT             = 64 / US_TO_CNT_SCALE_TX;   // Short pulse nominal time
uint8_t TX_LONG              = 128 / US_TO_CNT_SCALE_TX;    // Long pulse nominal time
uint8_t TX_SOF               = 200 / US_TO_CNT_SCALE_TX;    // Start Of Frame nominal time
uint8_t TX_EOD               = 200 / US_TO_CNT_SCALE_TX;    // End Of Data nominal time
uint8_t TX_EOF               = 280 / US_TO_CNT_SCALE_TX;    // End Of Frame nominal time
uint8_t TX_BRK               = 300 / US_TO_CNT_SCALE_TX;    // Break nominal time
uint8_t TX_IFS               = 280/ US_TO_CNT_SCALE_TX;    // Inter Frame Separation nominal time

uint8_t j1850_rx_buffer2[100];
boolean j1850_rx_readyToSendMsg = false;

byte j1850_rx_buffer[100];
uint8_t j1850_rx_currentBit = 8;
uint8_t j1850_rx_currentByte = 0;
boolean j1850_rx_messageStarted = false;
volatile uint16_t j1850_rx_lasttime;

byte j1850_tx_buffer[100];
uint8_t j1850_tx_buffer_len = 0;
uint8_t j1850_tx_currentBit = 8;
uint8_t j1850_tx_currentByte = 0;
boolean j1850_tx_pending = false;
boolean j1850_tx_messageStarted = false;

volatile byte j1850_bus_state = LOW;
int j1850_bus_driven_state = 0;

int j1850_pin;
uint32_t lastTime;

void j1850_pin_onChange()
{
	uint32_t newTime = micros();
	uint32_t pulsewidth = lastTime - newTime;
	newTime = lastTime;
	TCNT1 = 0;
	j1850_bus_state = !j1850_bus_state;
	if(j1850_bus_state == HIGH)
	{
		if(j1850_rx_messageStarted)
		{
			handleIncomingPulse(LOW, pulsewidth);
		}
	}
	else if(j1850_bus_state == LOW)
	{
		if(!j1850_rx_messageStarted && pulsewidth > RX_SOF_MIN && pulsewidth <= RX_SOF_MAX)
		{
			j1850_rx_messageStarted = true;
			j1850_rx_currentBit = 8;
			j1850_rx_currentByte = 0;
			j1850_rx_buffer[0] = 0;
		}
		else if(j1850_rx_messageStarted)
		{
			handleIncomingPulse(HIGH, pulsewidth);
		}
	}
}

void handleIncomingPulse(int busState, int pulseWidth)
{
	if(pulseWidth > RX_SHORT_MIN && pulseWidth <= RX_SHORT_MAX)
	{
		pushReceivedBit(busState == HIGH ? 1 : 0);
	}
	else if(pulseWidth > RX_LONG_MIN && pulseWidth <= RX_LONG_MAX)
	{
		pushReceivedBit(busState == LOW ? 1 : 0);
	}
	else //we're off in the woods. There's wolves about.
	{
		Serial.println(pulseWidth);
		j1850_rx_messageStarted = false;
		j1850_rx_currentBit = 8;
		j1850_rx_currentByte = 0;
		j1850_rx_buffer[0] = 0;
	}
}

inline void pushReceivedBit(int rxBit)
{
	j1850_rx_buffer[j1850_rx_currentByte] |= rxBit << (j1850_rx_currentBit - 1);
	if(j1850_rx_currentBit > 1) 
	{
		j1850_rx_currentBit--;
	}
	else
	{
		j1850_rx_currentBit = 8;
		j1850_rx_currentByte++;
		j1850_rx_buffer[j1850_rx_currentByte] = 0;
	}
}

void J1850VPW::setup(int pin)
{
	j1850_pin = pin;

	cli();//stop interrupts

	pinMode(j1850_pin, OUTPUT);
	digitalWrite(j1850_pin, LOW);
	j1850_bus_state = digitalRead(j1850_pin);
	attachInterrupt(digitalPinToInterrupt(2), j1850_pin_onChange, CHANGE);
	
	TCCR2A = 0;
	TCNT2 = 0;
	TCCR2B = (1 << WGM12) | (1 << CS12); // CTC mode, prescaler 64
	
	OCR1B = RX_EOF_MIN / 4; 
	TIMSK2 |= (1 << OCIE2B); //enable the compare interrupt

	sei();//allow interrupts
}

bool J1850VPW::readBytes(uint8_t *buffer, uint8_t &len)
{
	if(j1850_rx_readyToSendMsg && j1850_rx_buffer2[0] <= len)
	{
		memcpy(buffer, j1850_rx_buffer2 + 1, j1850_rx_buffer2[0]);
		len = j1850_rx_buffer2[0];
		j1850_rx_readyToSendMsg = false;
		return true;
	}
	return false;
}

bool J1850VPW::sendBytes(uint8_t *buffer, uint8_t len)
{
	memcpy(j1850_tx_buffer, buffer, len);
	j1850_tx_buffer[len] = crc8(j1850_tx_buffer, len);
	j1850_tx_buffer_len = len + 1;
	j1850_tx_currentBit = 8;
	j1850_tx_currentByte = 0;
	OCR2A = TX_IFS;
	TCNT2 = 0;
	TIMSK2 |= (1 << OCIE2A); //enable the compare interrupt
	j1850_tx_pending = true;
	return true;
}

uint8_t crc8(uint8_t *buffer, uint8_t len)
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

ISR(TIMER2_COMPB_vect)
{
	j1850_bus_state = digitalRead(j1850_pin);
	if(j1850_bus_state == LOW && j1850_rx_messageStarted)
	{
		j1850_rx_messageStarted = false;
		int len = (j1850_rx_currentBit == 8 ? j1850_rx_currentByte - 1 : j1850_rx_currentByte);
		j1850_rx_buffer2[0] = len;
		memcpy(j1850_rx_buffer2 + 1, j1850_rx_buffer, len + 1);
		j1850_rx_readyToSendMsg = true;
		j1850_rx_currentBit = 8;
		j1850_rx_currentByte = 0;
		j1850_rx_buffer[0] = 0;
	}
}

ISR(TIMER2_COMPA_vect)
{
	if(!j1850_tx_pending)
	{
		return;
	}
	if(!j1850_tx_messageStarted)
	{
		j1850_tx_messageStarted = true;
		j1850_bus_driven_state = HIGH;
		OCR2A = TX_SOF;
		TCNT2 = 0;
		digitalWrite(j1850_pin, j1850_bus_driven_state);
		return;
	}
	if(j1850_bus_state != j1850_bus_driven_state)
	{
		j1850_tx_messageStarted = false;
		j1850_tx_currentBit = 8;
		j1850_tx_currentByte = 0;
		j1850_bus_driven_state = LOW;
		OCR2A = TX_IFS;
		TCNT2 = 0;
		return;
	}
	if(j1850_tx_currentByte == j1850_tx_buffer_len)
	{
		TIMSK2 &= ~(1 << OCIE2A); //disable the compare interrupt on timer 2, AKA message sending has completed.
		
		j1850_bus_driven_state = LOW;
		j1850_tx_pending = false;
		j1850_tx_messageStarted = false;

		digitalWrite(j1850_pin, j1850_bus_driven_state);
		return;
	}
	uint8_t bitValue = j1850_tx_buffer[j1850_tx_currentByte] & (1 << (j1850_tx_currentBit - 1));
	j1850_bus_driven_state = !j1850_bus_driven_state;
	if(j1850_bus_driven_state == LOW)
	{
		OCR2A = bitValue ? TX_LONG : TX_SHORT;
	}
	else if(j1850_bus_driven_state == HIGH)
	{
		OCR2A = bitValue ? TX_SHORT : TX_LONG ;
	}
	//writeBit(j1850_bus_driven_state, OCR2A * 8);
	TCNT2 = 0;
	digitalWrite(j1850_pin, j1850_bus_driven_state);
	if(j1850_tx_currentBit > 1) j1850_tx_currentBit--;
	else
	{
		j1850_tx_currentByte++;
		j1850_tx_currentBit = 8;
	}
}