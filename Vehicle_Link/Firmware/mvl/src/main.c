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

#define CMD_NOOP				0
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

void usb_setup_tx_buffer(struct byte_buffer *usbBuffer, struct byte_buffer *txBuffer);
uint8_t *usb_rx_get_next(uint8_t *ptr);

void usart_setup(USART_t* usart, uint32_t baud)
{
	sysclk_enable_peripheral_clock(usart);
	usart_set_mode(usart, USART_CMODE_ASYNCHRONOUS_gc);
	usart_format_set(usart, USART_CHSIZE_8BIT_gc, USART_PMODE_DISABLED_gc, false);
	usart_set_baudrate(usart, baud, sysclk_get_per_hz());
	usart_set_rx_interrupt_level(usart, USART_INT_LVL_LO);
	usart_tx_enable(usart);
	usart_rx_enable(usart);
}

uint8_t usb_rx_ring_buffer[1024];
uint16_t usb_rx_buffer_fill_size = 0;
uint8_t *usb_rx_begin = usb_rx_ring_buffer, *usb_rx_last = usb_rx_ring_buffer;


uint8_t *usb_rx_get_next(uint8_t *ptr)
{
	if(ptr == (usb_rx_ring_buffer + 1024)) return usb_rx_ring_buffer;
	else return (ptr + 1);
}

void set_mux_config(uint8_t cmd)
{
	PORTB.OUTCLR = (PIN_SCI_A_ENGINE_RX_EN | PIN_SCI_B_ENGINE_RX_EN | PIN_SCI_A_TRANS_RX_EN | PIN_SCI_B_TRANS_RX_EN);
	
	PORTE.OUTSET = PIN_SCI_A_TX_EN;
	
	PORTD.OUTSET =  PIN_ISO_K_EN ;
	switch(cmd)
	{
		case CMD_SET_MUX_SCIAE:
			PORTB.OUTSET = PIN_SCI_A_ENGINE_RX_EN;
			PORTE.OUTCLR = PIN_SCI_A_TX_EN;
			break;
		case CMD_SET_MUX_SCIAT:
			PORTB.OUTSET = PIN_SCI_A_TRANS_RX_EN;
			PORTE.OUTCLR = PIN_SCI_A_TX_EN;
			break;
		case CMD_SET_MUX_SCIBE:
			PORTB.OUTSET = PIN_SCI_B_ENGINE_RX_EN;
			break;
		case CMD_SET_MUX_SCIBT:
			PORTB.OUTSET = PIN_SCI_B_TRANS_RX_EN;
			break;
		case CMD_SET_MUX_9141:
			PORTD.OUTCLR = PIN_ISO_K_EN;
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
	set_mux_config(CMD_NOOP); // reset all enable pins to default values
	
	// globally enable low-level interrupts in the PMIC.
	PMIC.CTRL = PMIC_LOLVLEN_bm;
	sci_setup();
	j1850vpw_setup();
	iso9141_setup();
	ccd_setup();
	sei();
	struct byte_buffer pendingTxJ1850, pendingTxIso9141, pendingTxCcd, pendingTxSci;
	pendingTxJ1850.idxLast = pendingTxIso9141.idxLast = pendingTxCcd.idxLast = pendingTxSci.idxLast = 0;
	struct byte_buffer pendingTxUsb, pendingRxUsb;
	pendingTxUsb.idxLast = pendingTxUsb.idxCurr = pendingRxUsb.idxLast = 0;
	while(1)
	{
		j1850vpw_do_tasks(&pendingTxJ1850);
		iso9141_do_tasks(&pendingTxIso9141);
		ccd_do_tasks(&pendingTxCcd);
		sci_do_tasks(&pendingTxSci);
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
						sci_hi_speed();
						break;
					case CMD_SET_SCI_LOSPEED:
						sci_lo_speed();
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
				if(pendingRxUsb.idxLast > 2)
				{
					if(pendingRxUsb.bytes[1] == PAYLOAD_PROTOCOL_J1850) usb_setup_tx_buffer(&pendingRxUsb, &pendingTxJ1850);
					else if(pendingRxUsb.bytes[1] == PAYLOAD_PROTOCOL_ISO9141) usb_setup_tx_buffer(&pendingRxUsb, &pendingTxIso9141);
					else if(pendingRxUsb.bytes[1] == PAYLOAD_PROTOCOL_CCD) usb_setup_tx_buffer(&pendingRxUsb, &pendingTxCcd);
					else if(pendingRxUsb.bytes[1] == PAYLOAD_PROTOCOL_SCI) usb_setup_tx_buffer(&pendingRxUsb, &pendingTxSci);
				}
				pendingRxUsb.idxLast = 0;
			}
		}
		if(udi_cdc_is_tx_ready() && usb_rx_begin != usb_rx_last)
		{
			udi_cdc_putc(*usb_rx_begin);
			// set idxLast to zero, this indicates buffer empty
			usb_rx_begin = usb_rx_get_next(usb_rx_begin);
			usb_rx_buffer_fill_size--;
		}
		if(udi_cdc_is_rx_ready())
		{
			pendingRxUsb.bytes[pendingRxUsb.idxLast] = (uint8_t) udi_cdc_getc();
			pendingRxUsb.idxLast++;
		}
		sleepmgr_enter_sleep();
	}
}

void usb_setup_tx_buffer(struct byte_buffer *usbBuffer, struct byte_buffer *txBuffer)
{
	memcpy(txBuffer->bytes, usbBuffer->bytes + 2, usbBuffer->idxLast - 2);
	txBuffer->idxLast = usbBuffer->idxLast - 2;
	txBuffer->idxCurr = 0;
}

void usb_queue_rx(uint8_t *srcBuffer, uint8_t srcBufferLen, uint8_t protocol)
{
	// set highest bit to indicate payload message, lower 7 bits is message length
	if(srcBufferLen + usb_rx_buffer_fill_size > 1024)
	{
		// buffer overflow
		usb_rx_begin = usb_rx_last;
		usb_rx_buffer_fill_size = 0;
	}
	*usb_rx_last = 0x80 | (srcBufferLen + 1);
	usb_rx_last = usb_rx_get_next(usb_rx_last);
	*usb_rx_last = protocol;
	usb_rx_last = usb_rx_get_next(usb_rx_last);
	for(uint8_t index = 0; index < srcBufferLen; index++)
	{
		*usb_rx_last = srcBuffer[index];
		usb_rx_last = usb_rx_get_next(usb_rx_last);
	}
	usb_rx_buffer_fill_size += srcBufferLen;
}