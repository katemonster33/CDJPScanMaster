/**
 * \file
 *
 * \brief Empty user application template
 *
 */

/**
 * \mainpage User Application template doxygen documentation
 *
 * \par Empty user application template
 *
 * Bare minimum empty user application template
 *
 * \par Content
 *
 * -# Include the ASF header files (through asf.h)
 * -# "Insert system clock initialization code here" comment
 * -# Minimal main function that starts with a call to board_init()
 * -# "Insert application code here" comment
 *
 */

/*
 * Include header files for all drivers that have been imported from
 * Atmel Software Framework (ASF).
 */
/*
 * Support and FAQ: visit <a href="http://www.atmel.com/design-support/">Atmel Support</a>
 */
#include <asf.h>
#include <stdint-gcc.h>
#include "conf_usb.h"

void uart_setup(USART_t uart,int8_t bscale, uint16_t bsel);
void uart_enable(USART_t uart);
void uart_disable(USART_t uart);
void set_mux_config(uint8_t config);

#define BAUD_7812_BSCALE	0
#define BAUD_7812_BSEL		255
#define BAUD_62500_BSCALE	0
#define BAUD_62500_BSEL		31
#define BAUD_5_BSCALE		7
#define BAUD_5_BSEL			3124
#define BAUD_10400_BSCALE	-4
#define BAUD_10400_BSEL		3061
#define BAUD_115200_BSCALE	-6
#define BAUD_115200_BSEL	1047

#define PIN_SCI_A_TX_EN			PIN0_bm // PORT E, active low
#define PIN_SCI_A_ENGINE_RX_EN	PIN0_bm // PORT B, active high
#define PIN_SCI_B_ENGINE_RX_EN	PIN1_bm // PORT B, active high
#define PIN_SCI_A_TRANS_RX_EN	PIN2_bm // PORT B, active high
#define PIN_SCI_B_TRANS_RX_EN	PIN3_bm // PORT B, active high
#define PIN_ISO_K_EN			PIN5_bm // PORT D, active low

#define PIN_J1850VPW_RX			PIN0_bm // PORT B
#define PIN_J1850VPW_TX			PIN1_bm // PORT B

enum MUX_SCI_ISO9141_CONFIG
{
	MUX_CONFIG_NONE = 0,
	MUX_CONFIG_SCI_A_ENGINE,
	MUX_CONFIG_SCI_A_TRANS,
	MUX_CONFIG_SCI_B_ENGINE,
	MUX_CONFIG_SCI_B_TRANS,
	MUX_CONFIG_ISO9141
};

struct uart_buffer
{
	char readBuffer[64];
	char *currentReadChar;
	char *lastReadChar;
	char writeBuffer[64];
	char *currentWriteChar;	
	char *lastWriteChar;
};

#define UART_CCD		USARTC0
struct uart_buffer ccdBuffer;
#define UART_PC			USARTD1
struct uart_buffer pcBuffer;
#define UART_ISO9141	USARTD0
struct uart_buffer isoBuffer;
#define UART_SCI		USARTE0
struct uart_buffer sciBuffer;

struct uart_buffer j1850Buffer;

static volatile bool main_b_cdc_enable = false;

void uart_setup(USART_t uart, int8_t bscale, uint16_t bsel)
{
	uart.CTRLA = 0;
	uart.CTRLB = 0;
	uart.CTRLC = USART_CHSIZE_8BIT_gc; // 8 bits, no parity, 1 stop bit
	uart.BAUDCTRLA = (bscale << 4) | ((bsel >> 8) & 0x0F);
	uart.BAUDCTRLB = (uint8_t)bsel;
	uart.STATUS = 0; // clear any lingering error bits just in case
}

void uart_enable(USART_t uart)
{
	// turn these bits on
	uart.CTRLB |= (USART_RXEN_bm | USART_TXEN_bm);
}

void uart_disable(USART_t uart)
{
	// turn these bits off
	uart.CTRLB &= ~(USART_RXEN_bm | USART_TXEN_bm);
}

void set_mux_config(uint8_t config)
{
	PORTB.OUTCLR = (PIN_SCI_A_ENGINE_RX_EN | PIN_SCI_B_ENGINE_RX_EN | PIN_SCI_A_TRANS_RX_EN | PIN_SCI_B_TRANS_RX_EN);
	PORTE.OUTSET = PIN_SCI_A_TX_EN;
	PORTD.OUTSET = PIN_ISO_K_EN;
	switch(config)
	{
		case MUX_CONFIG_SCI_A_ENGINE:
			PORTB.OUTSET = PIN_SCI_A_ENGINE_RX_EN;
			PORTE.OUTCLR = PIN_SCI_A_TX_EN;
			break;
		case MUX_CONFIG_SCI_A_TRANS:
			PORTB.OUTSET = PIN_SCI_A_TRANS_RX_EN;
			PORTE.OUTCLR = PIN_SCI_A_TX_EN;
			break;
		case MUX_CONFIG_SCI_B_ENGINE:
			PORTB.OUTSET = PIN_SCI_B_ENGINE_RX_EN;
			break;
		case MUX_CONFIG_SCI_B_TRANS:
			PORTB.OUTSET = PIN_SCI_B_TRANS_RX_EN;
			break;
		case MUX_CONFIG_ISO9141:
			PORTD.OUTCLR = PIN_ISO_K_EN;
			break;
		case MUX_CONFIG_NONE:
		default:
			break;
	}
}

int main (void)
{
	
	irq_initialize_vectors();
	cpu_irq_enable();

	// Initialize the sleep manager
	sleepmgr_init();
	/* Insert system clock initialization code here (sysclk_init()). */
	sysclk_init();
	board_init();
	udc_start();
	PORTB.DIRSET = PIN_SCI_A_ENGINE_RX_EN | PIN_SCI_B_ENGINE_RX_EN | PIN_SCI_A_TRANS_RX_EN | PIN_SCI_B_TRANS_RX_EN | PIN_J1850VPW_TX;
	PORTD.DIRSET = PIN_ISO_K_EN;
	PORTE.DIRSET = PIN_SCI_A_TX_EN;
	
	set_mux_config(MUX_CONFIG_NONE); // reset all enable pins to default values
	
	uart_setup(UART_CCD, BAUD_7812_BSCALE, BAUD_7812_BSEL);
	uart_setup(UART_SCI, BAUD_7812_BSCALE, BAUD_7812_BSEL);
	uart_setup(UART_ISO9141, BAUD_10400_BSCALE, BAUD_10400_BSEL);
	uart_setup(UART_PC, BAUD_115200_BSCALE, BAUD_115200_BSEL);
	uart_enable(UART_PC);
	
	/* Insert application code here, after the board has been initialized. */
	while(1)
	{
		if(udi_cdc_is_tx_ready()) 
		{
			
		}
		if(udi_cdc_is_rx_ready())
		{
			
		}
		sleepmgr_enter_sleep();
	}
}