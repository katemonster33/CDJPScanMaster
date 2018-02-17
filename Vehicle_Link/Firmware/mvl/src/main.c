/*
 * main.c
 *
 * Created: 2/4/2018
 *  Author: Katie M
 */
#include <asf.h>
#include <stdint-gcc.h>
#include "conf_usb.h"
#include "usb_protocol_cdc.h"
#include "main.h"
#include "string.h"

void usb_setup_rx_buffer(struct byte_buffer *usbBuffer, struct byte_buffer *srcBuffer, uint8_t protocol);

void usart_setup(USART_t* usart, int8_t bscale, uint16_t bsel)
{
	sysclk_enable_peripheral_clock(usart);
	usart_set_mode(usart, USART_CMODE_ASYNCHRONOUS_gc);
	usart_format_set(usart, USART_CHSIZE_8BIT_gc, USART_PMODE_DISABLED_gc, false);
	usart->BAUDCTRLA = (bscale << 4) | ((bsel >> 8) & 0x0F);
	usart->BAUDCTRLB = (uint8_t)bsel;
	usart_tx_enable(usart);
	usart_rx_enable(usart);
}

void byte_buffer_putchar(struct byte_buffer *buffer, uint8_t ch)
{
	if(buffer->idxLast >= BYTE_BUFFER_SIZE) return;
	buffer->bytes[buffer->idxLast] = ch;
	buffer->idxLast++;
}

void set_mux_config(MUX_CONFIG_t config)
{
	PORTB.OUTCLR = (PIN_SCI_A_ENGINE_RX_EN | PIN_SCI_B_ENGINE_RX_EN | PIN_SCI_A_TRANS_RX_EN | PIN_SCI_B_TRANS_RX_EN);
	
	PORTE.OUTSET = PIN_SCI_A_TX_EN;
	
	PORTD.OUTSET =  PIN_ISO_K_EN ;
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
	PORTB.DIRSET = PIN_SCI_A_ENGINE_RX_EN | PIN_SCI_B_ENGINE_RX_EN | PIN_SCI_A_TRANS_RX_EN | PIN_SCI_B_TRANS_RX_EN;
	PORTC.OUTSET = PIN_CCD_TX;
	PORTD.DIRSET = PIN_ISO_K_EN;
	PORTE.DIRSET = PIN_SCI_A_TX_EN;
	
	set_mux_config(MUX_CONFIG_NONE); // reset all enable pins to default values
	
	usart_setup(&UART_CCD, BAUD_7812_BSCALE, BAUD_7812_BSEL);
	usart_setup(&UART_SCI, BAUD_7812_BSCALE, BAUD_7812_BSEL);
	j1850vpw_setup();
	iso9141_setup();
	struct byte_buffer pendingRxJ1850, pendingRxIso9141, pendingRxCcd, pendingRxSci;
	struct byte_buffer pendingTxUsb, pendingRxUsb;
	pendingTxUsb.idxLast = pendingRxUsb.idxLast = 0;
	while(1)
	{
		pendingRxJ1850.idxLast = pendingRxIso9141.idxLast = pendingRxCcd.idxLast = pendingRxSci.idxLast = 0;
		j1850vpw_do_tasks(&pendingRxJ1850, 0);
		iso9141_do_tasks(&pendingRxIso9141, 0);
		if(pendingTxUsb.idxLast == 0)
		{
			if(pendingRxJ1850.idxLast != 0)			usb_setup_rx_buffer(&pendingTxUsb, &pendingRxJ1850, 0);
			else if(pendingRxIso9141.idxLast != 0)	usb_setup_rx_buffer(&pendingTxUsb, &pendingRxIso9141, 1);
			else if(pendingRxCcd.idxLast != 0)		usb_setup_rx_buffer(&pendingTxUsb, &pendingRxCcd, 2);
			else if(pendingRxSci.idxLast != 0)		usb_setup_rx_buffer(&pendingTxUsb, &pendingRxSci, 3);
		}
		if(pendingRxUsb.idxLast != 0 && ((pendingRxUsb.bytes[0] & 0x80) == 0 || pendingRxUsb.idxLast == ((pendingRxUsb.bytes[0] & 0x7F) + 1)))
		{
			// USB RX buffer contains a full request, parse logic goes here.
			pendingRxUsb.idxLast = 0;
		}
		if(udi_cdc_is_tx_ready() && pendingTxUsb.idxLast != 0)
		{
			iram_size_t bytesRemaining = udi_cdc_write_buf(pendingTxUsb.bytes + pendingTxUsb.idxCurr, pendingTxUsb.idxLast - pendingTxUsb.idxCurr);
			pendingTxUsb.idxCurr = pendingTxUsb.idxLast - bytesRemaining;
			// set idxLast to zero, this indicates buffer empty
			if(pendingTxUsb.idxCurr >= pendingTxUsb.idxLast) pendingTxUsb.idxLast = 0;
		}
		if(udi_cdc_is_rx_ready())
		{
			pendingRxUsb.bytes[pendingRxUsb.idxLast] = (uint8_t) udi_cdc_getc();
			pendingRxUsb.idxLast++;
		}
		sleepmgr_enter_sleep();
	}
}

void usb_setup_rx_buffer(struct byte_buffer *usbBuffer, struct byte_buffer *srcBuffer, uint8_t protocol)
{
	usbBuffer->idxLast = srcBuffer->idxLast + 2;
	usbBuffer->idxCurr = 0;
	// set highest bit to indicate payload message, lower 7 bits is message length
	usbBuffer->bytes[0] = 0x80 | (srcBuffer->idxLast + 1);
	usbBuffer->bytes[1] = protocol;
	memcpy(usbBuffer->bytes + 2, srcBuffer->bytes, srcBuffer->idxLast);
}