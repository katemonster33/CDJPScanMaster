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

#define PAYLOAD_FLAG 0x80

#define CMD_SET_MUX_SCIAE		1
#define CMD_SET_MUX_SCIAT		2
#define CMD_SET_MUX_SCIBE		3
#define CMD_SET_MUX_SCIBT		4
#define CMD_SET_MUX_9141		5
#define CMD_SET_SCI_HISPEED		6
#define CMD_SET_SCI_LOSPEED		7
#define CMD_ISO9141_5BAUDINIT	8
#define CMD_ISO9141_FASTINIT	9

static volatile bool main_b_cdc_enable = false;

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
	PORTD.DIRSET = PIN_ISO_K_EN;
	PORTE.DIRSET = PIN_SCI_A_TX_EN;
	PORTR.DIRSET |= PIN0_bm | PIN1_bm;
	PORTR.OUTSET |= PIN0_bm;
	set_mux_config(MUX_CONFIG_NONE); // reset all enable pins to default values
	
	sci_setup_lo_speed();
	j1850vpw_setup();
	iso9141_setup();
	ccd_setup();
	struct byte_buffer pendingRxJ1850, pendingRxIso9141, pendingRxCcd, pendingRxSci;
	struct byte_buffer pendingTxUsb, pendingRxUsb;
	pendingTxUsb.idxLast = pendingRxUsb.idxLast = 0;
	while(1)
	{
		pendingRxJ1850.idxLast = pendingRxIso9141.idxLast = pendingRxCcd.idxLast = pendingRxSci.idxLast = 0;
		j1850vpw_do_tasks(&pendingRxJ1850, 0);
		iso9141_do_tasks(&pendingRxIso9141, 0);
		ccd_do_tasks(&pendingRxCcd, 0);
		sci_do_tasks(&pendingRxSci, 0);
		// if we aren't currently sending anything via USB, check the protocol buffers for something to send.
		if(pendingTxUsb.idxLast == 0)
		{
			if(pendingRxJ1850.idxLast != 0)			usb_setup_rx_buffer(&pendingTxUsb, &pendingRxJ1850, 0);
			else if(pendingRxIso9141.idxLast != 0)	usb_setup_rx_buffer(&pendingTxUsb, &pendingRxIso9141, 1);
			else if(pendingRxCcd.idxLast != 0)		usb_setup_rx_buffer(&pendingTxUsb, &pendingRxCcd, 2);
			else if(pendingRxSci.idxLast != 0)		usb_setup_rx_buffer(&pendingTxUsb, &pendingRxSci, 3);
		}
		if(pendingRxUsb.idxLast != 0)
		{
			if((pendingRxUsb.bytes[0] & 0x80) == 0) // not a payload request, 1-byte command
			{
				switch(pendingRxUsb.bytes[0])
				{
					case CMD_SET_MUX_SCIAE:
					case CMD_SET_MUX_SCIAT:
					case CMD_SET_MUX_SCIBE:
					case CMD_SET_MUX_SCIBT:
					case CMD_SET_MUX_9141:
						set_mux_config(pendingRxUsb.bytes[0]);
						break;
					case CMD_SET_SCI_HISPEED:
						sci_setup_hi_speed();
						break;
					case CMD_SET_SCI_LOSPEED:
						sci_setup_lo_speed();
						break;
					case CMD_ISO9141_5BAUDINIT:
						// TO DO
						break;
					case CMD_ISO9141_FASTINIT:
						// TO DO
						break;
				}
				pendingRxUsb.idxLast = 0;
			}
			else if(pendingRxUsb.idxLast == ((pendingRxUsb.bytes[0] & 0x7F) + 1))
			{
				pendingRxUsb.idxLast = 0;
			}
		}
		if(udi_cdc_is_tx_ready() && pendingTxUsb.idxLast != 0)
		{
			udi_cdc_putc(pendingTxUsb.bytes[pendingTxUsb.idxCurr]);
			pendingTxUsb.idxCurr++;
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