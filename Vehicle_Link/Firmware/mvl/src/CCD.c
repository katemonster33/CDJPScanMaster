/*
 * CCD.c
 *
 * Created: 3/2/2018 12:04:17 AM
 *  Author: rk5642
 */
 #include "main.h"
 #include "usart.h"
#include "string.h"

#define UART_CCD		USARTC0
#define CCD_IDLE_TIMER TCC0
#define CCD_TX0_TIMER TCC1

#define PIN_CCD_RX				PIN2_bm // PORT C
#define PIN_CCD_TX				PIN3_bm // PORT C

void ccd_idle_timer_stop(void);
void ccd_idle_timer_start(void);
void ccd_idle_timer_setup(void);
void ccd_idle_timer_callback(void);

void ccd_tx0_timer_stop(void);
void ccd_tx0_timer_start(void);
void ccd_tx0_timer_setup(void);
void ccd_tx0_timer_tick(void);

uint8_t ccd_calculate_crc(uint8_t *buff, uint8_t len);

#define IDLE_BUS_TIME (11 * 128 / 2) // time for bus to be considered idle = 11 bits (normalized for current baud rate)

// 0 = start bit
// 1-8 = data
// 9 = stop bit
uint8_t ccd_tx0_current_bit = 0;
uint8_t ccd_tx0_byte = 0;
// TX0 timer ticks 4 times per UART bit. The steps in between are for checking the current bus status. This checks where we are.
uint8_t ccd_tx0_step_index = 0;

bool ccd_bus_idle = false;
uint8_t ccd_rx_buffer[32];
uint8_t ccd_rx_buffer_len = 0;
uint8_t ccd_tx_buffer[32];
uint8_t ccd_tx_buffer_len = 0;
uint8_t ccd_tx_current_byte = 0;
bool ccd_tx_active = false;

void ccd_idle_timer_stop()
{
	tc_write_clock_source(&CCD_IDLE_TIMER, TC_CLKSEL_OFF_gc);
	tc_disable_cc_channels(&CCD_IDLE_TIMER, TC_CCAEN);
}

void ccd_idle_timer_start()
{
	tc_enable_cc_channels(&CCD_IDLE_TIMER, TC_CCAEN);
	CCD_IDLE_TIMER.CNT = 0;                        // Init: Counter register
	tc_write_clock_source(&CCD_IDLE_TIMER, TC_CLKSEL_DIV64_gc);
}

void ccd_idle_timer_setup()
{
	tc_enable(&CCD_IDLE_TIMER);
	CCD_IDLE_TIMER.CNT = 0;
	tc_set_wgm(&CCD_IDLE_TIMER, TC_WG_NORMAL);
	tc_write_period(&CCD_IDLE_TIMER, 30000);
	tc_set_cca_interrupt_level(&CCD_IDLE_TIMER, TC_INT_LVL_LO);
	tc_set_cca_interrupt_callback(&CCD_IDLE_TIMER, ccd_idle_timer_callback);
	tc_write_cc(&CCD_IDLE_TIMER, TC_CCA, IDLE_BUS_TIME);
}

void ccd_tx0_timer_stop()
{
	CCD_TX0_TIMER.CTRLA = TC_CLKSEL_OFF_gc;
	PORTC.INT0MASK |= PIN2_bm;
}

void ccd_tx0_timer_start()
{
	PORTC.INT0MASK &= ~PIN2_bm;
	ccd_bus_idle = false;
	CCD_TX0_TIMER.CNT = 0;
	CCD_TX0_TIMER.CTRLA = TC_CLKSEL_DIV64_gc; // 32 Mhz / 64 = 2 uS per tick
}

void ccd_tx0_timer_setup()
{
	tc_enable(&CCD_TX0_TIMER);
	tc_set_wgm(&CCD_TX0_TIMER, TC_WG_NORMAL);
	CCD_TX0_TIMER.CNT = 0;
	CCD_TX0_TIMER.CTRLA = TC_CLKSEL_OFF_gc;
	CCD_TX0_TIMER.CTRLB = 0; // no waveform generation, no compare/capture
	CCD_TX0_TIMER.PER = 4; // 4 = 8 uS / 2 uS per timer tick
	CCD_TX0_TIMER.INTCTRLA = TC_ERRINTLVL_OFF_gc | TC_OVFINTLVL_LO_gc;
	CCD_TX0_TIMER.INTCTRLB = 0x00; // CCA/CCB/etc interrupts off;
	
	tc_set_overflow_interrupt_callback(&CCD_TX0_TIMER, ccd_tx0_timer_tick);
}

void ccd_idle_timer_callback()
{
	uint8_t checksum = ccd_calculate_crc(ccd_rx_buffer, ccd_rx_buffer_len - 1);
	if(checksum == ccd_rx_buffer[ccd_rx_buffer_len - 1])
	{
		bt_queue_rx(ccd_rx_buffer, ccd_rx_buffer_len - 1, PAYLOAD_PROTOCOL_CCD);	
	}
	ccd_rx_buffer_len = 0;
	ccd_bus_idle = true;
	ccd_idle_timer_stop();
	if(ccd_tx_buffer_len > 0 && !ccd_tx_active)
	{
		ccd_tx_active = true;
		ccd_tx0_timer_start();
	}
}

void ccd_tx0_timer_tick(void)
{
	if(ccd_tx0_step_index == 0)
	{
		if(ccd_tx_active)
		{
			if(ccd_tx0_current_bit == 0) PORTC.OUTCLR |= PIN_CCD_TX;
			else if(ccd_tx0_current_bit < 9)
			{
				if(ccd_tx0_byte & (1 << (8 - ccd_tx0_current_bit))) PORTC.OUTSET |= PIN_CCD_TX;
				else PORTC.OUTCLR |= PIN_CCD_TX;
			}
			// stop bit
			else if(ccd_tx0_current_bit == 9) PORTC.OUTSET |= PIN_CCD_TX;
		}
		// after stop bit - turn off timer
		if(ccd_tx0_current_bit == 10)
		{
			ccd_tx0_current_bit = 0;
			ccd_tx0_timer_stop();
			usart_tx_enable(&UART_CCD);
			usart_rx_enable(&UART_CCD);
			if(ccd_tx_buffer_len > 1 && ccd_tx_active)
			{
				UART_CCD.DATA = ccd_tx_buffer[1];
				ccd_tx_current_byte = 2;
			}
			if(!ccd_tx_active)
			{
				ccd_rx_buffer[0] = ccd_tx0_byte;
				ccd_rx_buffer_len = 1;
				ccd_idle_timer_start();
			}
		}
		ccd_tx0_step_index++;
	}
	else if(ccd_tx0_step_index < 3)
	{
		bool busActive = (PORTC.IN & PIN_CCD_RX) == 0;
		if(ccd_tx0_current_bit >= 1 && ccd_tx0_current_bit <= 8 && (ccd_tx0_byte & (1 << (8 - ccd_tx0_current_bit))) && busActive)
		{
			//collision detected, abort TX
			ccd_tx_active = false;
		}
		// we're not transmitting anymore so we must continue building the byte ourselves
		if(!ccd_tx_active) 
		{
			uint8_t bitMask = 1 << (8 - ccd_tx0_current_bit);
			if(busActive) ccd_tx0_byte &= ~bitMask;
			else ccd_tx0_byte |= bitMask;
		}
		ccd_tx0_step_index++;
	}
	else 
	{
		ccd_tx0_step_index = 0;
		ccd_tx0_current_bit++;
	}
}

uint8_t ccd_calculate_crc(uint8_t *buff, uint8_t len)
{
	uint8_t sum = 0;
	for(uint8_t *by = buff; by < (buff + len); by++)
	{
		sum += *by;
	}
	return sum;
}

void ccd_setup()
{
	PORTC.PIN2CTRL = PORT_ISC_FALLING_gc | PORT_OPC_TOTEM_gc;
	PORTC.INT0MASK |= PIN2_bm;
	PORTC.INTCTRL = PORT_INT0LVL_LO_gc;
	usart_setup(&UART_CCD, 7812);
	usart_set_tx_interrupt_level(&UART_CCD, USART_INT_LVL_LO);
	ccd_idle_timer_setup();
	ccd_tx0_timer_setup();
}

void ccd_do_tasks(struct byte_buffer *txBuffer)
{
	if(txBuffer->idxLast != 0)
	{
		memcpy(ccd_tx_buffer, txBuffer->bytes, txBuffer->idxLast);
		ccd_tx_buffer_len = txBuffer->idxLast;
		ccd_tx_buffer[ccd_tx_buffer_len] = ccd_calculate_crc(ccd_tx_buffer, ccd_tx_buffer_len);
		ccd_tx_buffer_len++;
		ccd_tx0_current_bit = 0;
		if(ccd_bus_idle) ccd_tx0_timer_start();
		
	}
}

ISR(USARTC0_DRE_vect)
{
	if(ccd_tx_current_byte < ccd_tx_buffer_len)
	{
		UART_CCD.DATA = ccd_tx_buffer[ccd_tx_current_byte];
		ccd_tx_current_byte++;	
	}
	else
	{
		ccd_bus_idle = true; // no need to start the idle timer, we know the bus is idle, we'll start the timer next time a byte is received
		ccd_tx_buffer_len = ccd_tx_current_byte = 0;
	}
}

ISR(USARTC0_RXC_vect)
{
	ccd_rx_buffer[ccd_rx_buffer_len] = UART_CCD.DATA;
	ccd_rx_buffer_len++;
	USARTC0.STATUS |= USART_RXCIF_bm;
	ccd_idle_timer_start();
}

ISR(PORTC_INT0_vect)
{
	ccd_idle_timer_stop();
	PORTC.INTFLAGS = PORT_INT0IF_bm;
}