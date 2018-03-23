/*
 * CCD.c
 *
 * Created: 3/2/2018 12:04:17 AM
 *  Author: rk5642
 */
 #include "main.h"
 #include "usart.h"

#define UART_CCD		USARTC0

void ccd_idle_timer_stop(void);
void ccd_idle_timer_start(void);
void ccd_idle_timer_setup(void);
void ccd_timer_callback(void);
uint8_t ccd_calculate_crc(uint8_t *buff, uint8_t len);

#define GET_BIT_TIME(X) (X * 128 / 2)
#define IDLE_BUS_TIME GET_BIT_TIME(11) // time for bus to be considered idle = 11 bits (normalized for current baud rate)
#define CCD_TIMER TCC0

bool drivingBus = false;

void ccd_idle_timer_stop()
{
	tc_write_clock_source(&CCD_TIMER, TC_CLKSEL_OFF_gc);
	tc_disable_cc_channels(&CCD_TIMER, TC_CCAEN);
}

void ccd_idle_timer_start()
{
	tc_enable_cc_channels(&CCD_TIMER, TC_CCAEN);
	CCD_TIMER.CNT = 0;                        // Init: Counter register
	tc_write_clock_source(&CCD_TIMER, TC_CLKSEL_DIV64_gc);
}

void ccd_idle_timer_setup()
{
	tc_enable(&CCD_TIMER);
	tc_set_wgm(&CCD_TIMER, TC_WG_NORMAL);
	tc_write_period(&CCD_TIMER, 30000);
	tc_set_cca_interrupt_level(&CCD_TIMER, TC_INT_LVL_LO);
	tc_set_cca_interrupt_callback(&CCD_TIMER, ccd_timer_callback);
	tc_write_cc(&CCD_TIMER, TC_CCA, IDLE_BUS_TIME);
	CCD_TIMER.CNT = 0;                         // Init: Counter register
}

uint8_t ccd_rx_buffer[255];
uint8_t ccd_rx_buffer_len = 0;
uint8_t diff = 0;

void ccd_timer_callback()
{
	uint8_t checksum = ccd_calculate_crc(ccd_rx_buffer, ccd_rx_buffer_len - 1);
	if(checksum == ccd_rx_buffer[ccd_rx_buffer_len - 1])
	{
		usb_queue_rx(ccd_rx_buffer, ccd_rx_buffer_len - 1, PAYLOAD_PROTOCOL_CCD);	
	}
	else
	{
		diff = ccd_rx_buffer[ccd_rx_buffer_len - 1] - checksum;
	}
	ccd_rx_buffer_len = 0;
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
	PORTC.DIRSET = PIN_CCD_TX;
	PORTC.PIN2CTRL = PORT_ISC_RISING_gc | PORT_OPC_TOTEM_gc;
	PORTC.INT0MASK = PIN2_bm;
	PORTC.INTCTRL = PORT_INT0LVL_LO_gc;
	PORTC.OUTSET = PIN_CCD_TX; // write it high immediately so we aren't driving the bus accidentally
	usart_setup(&UART_CCD, 7812);
	ccd_idle_timer_setup();
}

void ccd_do_tasks(struct byte_buffer *txBuffer)
{
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
	if(drivingBus)
	{
		drivingBus = false;
	}
	ccd_idle_timer_stop();
	PORTC.INTFLAGS = PORT_INT0IF_bm;
}
