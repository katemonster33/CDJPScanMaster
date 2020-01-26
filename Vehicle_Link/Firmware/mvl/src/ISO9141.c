/*
 * ISO9141_CCD_SCI.c
 *
 * Created: 2/5/2018 6:06:42 PM
 *  Author: Katie M
 */ 
#include "main.h"
#include "usart.h"
#include <string.h>

#define PIN_ISO9141_RX			PIN2_bm // PORT D
#define PIN_ISO9141_TX			PIN3_bm // PORT D

#define UART_ISO9141	USARTD0
#define ISO9141_TIMER TCD1

uint8_t iso9141_rx_buffer[64];
uint8_t iso9141_rx_buffer_len = 0;
uint8_t iso9141_tx_buffer[64];
uint8_t iso9141_tx_buffer_len = 0;
volatile uint8_t iso9141_tx_current_byte = 0;

volatile bool iso9141_init_active = false;
volatile bool iso9141_bus_idle = false;
void iso9141_timer_stop(void);
void iso9141_timer_start(const uint16_t tickValue, tc_callback_t callbackFunc);

void iso9141_idle_timeout_callback(void);
void iso9141_five_baud_callback(void);
void iso9141_fast_init_callback(void);
void iso9141_five_baud_sync_tx_callback(void);
void iso9141_five_baud_finish(uint8_t rc);
void iso9141_init_timeout_callback(void);
void iso9141_p4_min_timeout_callback(void);
uint8_t iso9141_checksum(uint8_t *buffer, uint8_t len);

const uint16_t five_baud_tick = 23500;		// 200 ms
const uint16_t p1_min = 0;				// 0 ms
const uint16_t p1_max = 20 * 125;		// 20 ms
const uint16_t p2_min = 25 * 125;		// 25 ms
const uint16_t p2_max = 50 * 125;		// 50 ms
const uint16_t p3_min = 55 * 125;		// 55 ms
const uint16_t p4_min = 433;			// ~3.46 ms
const uint16_t p4_max = 20 * 125;		// 20 ms
const uint16_t w0_min = 2 * 125;		// 2 ms
const uint16_t w1_max = 37500;		// 300 ms
const uint16_t w2_max = 20 * 125;		// 20 ms
const uint16_t w3_max = 20 * 125;		// 20 ms
const uint16_t w4_min = 20 * 125;		// 20 ms
const uint16_t w5_min = 50 * 125;		// 50 ms
const uint16_t tidle =	37500;		// 300 ms
const uint16_t tinil =	25 * 125;		// 25 ms
const uint16_t twup =	50 * 125;		// 50 ms

void iso9141_idle_timeout_callback(void)
{
	ISO9141_TIMER.INTFLAGS &= ~TC0_OVFIF_bm;
	iso9141_timer_stop();
	iso9141_bus_idle = true;
	if(iso9141_rx_buffer_len != 0)
	{
		bt_queue_rx(iso9141_rx_buffer, iso9141_rx_buffer_len, PAYLOAD_PROTOCOL_ISO9141);
		iso9141_rx_buffer_len = 0;	
	}
}

void iso9141_p4_min_timeout_callback(void)
{
	ISO9141_TIMER.INTFLAGS &= ~TC0_OVFIF_bm;
	iso9141_timer_stop();
	if(iso9141_tx_current_byte != 0)
	{
		UART_ISO9141.DATA = iso9141_tx_buffer[iso9141_tx_current_byte];
		iso9141_tx_current_byte++;
		if(iso9141_tx_current_byte == iso9141_tx_buffer_len) 
		{
			iso9141_tx_current_byte = 0;
			iso9141_tx_buffer_len = 0;
			iso9141_timer_start(tidle, iso9141_idle_timeout_callback);
		}
	}
}

void iso9141_timer_stop()
{
	ISO9141_TIMER.CTRLA = (ISO9141_TIMER.CTRLA & ~TC0_CLKSEL_gm) | TC_CLKSEL_OFF_gc;
}

void iso9141_timer_start(const uint16_t tickValue, tc_callback_t callbackFunc)
{
	iso9141_timer_stop();
	ISO9141_TIMER.CNT = 0;                        // Init: Counter register
	ISO9141_TIMER.PER = tickValue;
	tc_set_overflow_interrupt_callback(&ISO9141_TIMER, callbackFunc);
	ISO9141_TIMER.CTRLA = (ISO9141_TIMER.CTRLA & ~TC0_CLKSEL_gm) | TC_CLKSEL_DIV256_gc;
}

void iso9141_setup()
{
	iso9141_bus_idle = true;
	iso9141_rx_buffer_len = 0;
	usart_setup(&UART_ISO9141, 10400);
	tc_enable(&ISO9141_TIMER);
	ISO9141_TIMER.CTRLB = TC_WGMODE_NORMAL_gc ; // no waveform generation, no compare/capture
	ISO9141_TIMER.INTCTRLA = TC_OVFINTLVL_LO_gc; // overflow interrupt enabled - low level
}

void iso9141_do_tasks(struct byte_buffer *txBuffer)
{
	if(txBuffer->idxLast != 0)
	{
		memcpy(iso9141_tx_buffer, txBuffer->bytes, txBuffer->idxLast);
		iso9141_tx_buffer_len = txBuffer->idxLast;
		iso9141_tx_buffer[iso9141_tx_buffer_len] = iso9141_checksum(iso9141_tx_buffer, iso9141_tx_buffer_len);
		iso9141_tx_buffer_len++;
		iso9141_tx_current_byte = 0;
		txBuffer->idxLast = 0;
	}
	if(iso9141_tx_buffer_len != 0 && iso9141_tx_current_byte == 0 && iso9141_bus_idle)
	{
		UART_ISO9141.DATA = iso9141_tx_buffer[iso9141_tx_current_byte];
		iso9141_tx_current_byte++;
	}
}

volatile uint8_t iso9141_sync_byte = 0x33;
volatile uint8_t iso9141_sync_bit = 0;
volatile bool iso9141_sent_kw2_inverted = false;

void iso9141_start_five_baud_init()
{
	UART_ISO9141.CTRLB &= ~(USART_TXEN_bm | USART_RXEN_bm);
	PORTD.DIRSET |= PIN_ISO9141_TX;
	iso9141_sent_kw2_inverted = false;
	iso9141_sync_byte = 0x33;
	iso9141_sync_bit = 0;
	iso9141_init_active = true;
	PORTD.OUTCLR |= PIN_ISO9141_TX;
	iso9141_timer_start(five_baud_tick, iso9141_five_baud_sync_tx_callback);
}

void iso9141_five_baud_finish(uint8_t rc)
{
	UART_ISO9141.CTRLB |= (USART_TXEN_bm | USART_RXEN_bm);
	bt_queue_cmd(rc);
}

uint8_t iso9141_checksum(uint8_t *buffer, uint8_t len)
{
	uint8_t ret = 0;
	for (uint8_t i=0; i < len; i++){
		ret += buffer[i];
	}
	return ret;
}

void iso9141_five_baud_sync_tx_callback()
{
	ISO9141_TIMER.INTFLAGS &= ~TC0_OVFIF_bm;
	if(iso9141_sync_bit < 8)
	{
		if(iso9141_sync_byte & 1) PORTD.OUTSET |= PIN_ISO9141_TX;
		else PORTD.OUTCLR |= PIN_ISO9141_TX;
		iso9141_sync_byte >>= 1;
	}
	else if(iso9141_sync_bit == 8) PORTD.OUTSET |= PIN_ISO9141_TX;
	else if(iso9141_sync_bit == 9)
	{
		iso9141_timer_start(w1_max, iso9141_init_timeout_callback);	
	}
	iso9141_sync_bit++;
}

void iso9141_init_timeout_callback()
{
	ISO9141_TIMER.INTFLAGS &= ~TC0_OVFIF_bm;
	iso9141_timer_stop();
	if(iso9141_rx_buffer_len == 3 && !iso9141_sent_kw2_inverted) // Got SYNC / KW1 / KW2 - OK to continue
	{
		UART_ISO9141.DATA = ~iso9141_rx_buffer[2]; // Send KW2 inverted
		iso9141_sent_kw2_inverted = true;
		iso9141_timer_start(w4_min, iso9141_init_timeout_callback);
	}
	else
	{
		iso9141_init_active = false;
		iso9141_five_baud_finish(RC_FAIL);
	}
}

ISR(USARTD0_RXC_vect)
{
	uint8_t recvData = UART_ISO9141.DATA;
	UART_ISO9141.STATUS &= ~USART_RXCIF_bm;
	iso9141_rx_buffer[iso9141_rx_buffer_len] = recvData;
	iso9141_rx_buffer_len++;
	if(iso9141_init_active)
	{
		if(iso9141_rx_buffer_len == 1) // got SYNC response
		{
			if(recvData == 0x55) 
			{
				iso9141_timer_start(w2_max, iso9141_init_timeout_callback); 
			}
			else iso9141_rx_buffer_len--;
		}
		else if(iso9141_rx_buffer_len == 2) // received Keyword 1
		{
			if(recvData == 0x08 || recvData == 0x94 || recvData == 0x20) 
			{
				iso9141_timer_start(w3_max, iso9141_init_timeout_callback);
			}
			else iso9141_rx_buffer_len--;
		}
		else if(iso9141_rx_buffer_len == 3) // received Keyword 2
		{
			if (recvData == 0x08 || recvData == 0x94 || recvData == 0x25 || recvData == 0x27 || recvData == 0x29 || recvData == 0x31)
			{
				iso9141_timer_start(w4_min, iso9141_init_timeout_callback);
			}
			else iso9141_rx_buffer_len--;
		}
		else if(iso9141_rx_buffer_len == 4) // received Keyword 2 inverted
		{
			if(recvData == ~iso9141_rx_buffer[2]) iso9141_timer_start(w4_min, iso9141_init_timeout_callback); // check data. if sane, wait for address
			else iso9141_five_baud_finish(RC_FAIL); // else return failure
		}
		else if(iso9141_rx_buffer_len == 5) // received address
		{
			iso9141_init_active = false;
			if(recvData == 0xCC) // valid address
			{
				bt_queue_cmd(RC_SUCCESS);
				bt_queue_rx(iso9141_rx_buffer, iso9141_rx_buffer_len, PAYLOAD_PROTOCOL_ISO9141);
				iso9141_rx_buffer_len = 0;
				// bus will be considered idle when p3_min has elapsed
				iso9141_bus_idle = false;
				iso9141_timer_start(p3_min, iso9141_idle_timeout_callback);
			}
			else iso9141_five_baud_finish(RC_FAIL);
		}
	}
	else
	{
		iso9141_timer_start(p4_min, iso9141_p4_min_timeout_callback);
	}
}