/*
 * main.c
 *
 * Created: 2/4/2018
 *  Author: Katie M
 */
#include <asf.h>
#include <stdint-gcc.h>
#include "main.h"
#include "string.h"

#define PAYLOAD_FLAG 0x80

#define CMD_NOOP				0
#define CMD_SET_MUX_SCI_A		1
#define CMD_SET_MUX_SCI_B		2
#define CMD_SET_MUX_9141		5
#define CMD_SET_SCI_HISPEED		6
#define CMD_SET_SCI_LOSPEED		7
#define CMD_ISO9141_5BAUDINIT	8
#define CMD_ISO9141_FASTINIT	9

#define UART_BT		USARTD1

static volatile bool main_b_cdc_enable = false;

void bt_setup_tx_buffer(struct byte_buffer *btBuffer, struct byte_buffer *txBuffer);
uint8_t *bt_rx_get_next(uint8_t *ptr);
void bt_write_byte(uint8_t by);
void bt_write_hexchar(uint8_t nibble);

void usart_setup(USART_t* usart, uint32_t baud)
{
	sysclk_enable_peripheral_clock(usart);
	usart_set_mode(usart, USART_CMODE_ASYNCHRONOUS_gc);
	usart_format_set(usart, USART_CHSIZE_8BIT_gc, USART_PMODE_DISABLED_gc, false);
	usart_set_baudrate(usart, baud, sysclk_get_per_hz());
	usart_set_rx_interrupt_level(usart, USART_INT_LVL_LO);
	usart->CTRLB = usart->CTRLB | USART_TXEN_bm | USART_RXEN_bm;
}

uint8_t bt_rx_ring_buffer[1024];
uint16_t bt_rx_buffer_fill_size = 0;
uint8_t *bt_rx_begin = bt_rx_ring_buffer, *bt_rx_last = bt_rx_ring_buffer;

uint8_t *bt_rx_get_next(uint8_t *ptr)
{
	if(ptr == (bt_rx_ring_buffer + 1024)) return bt_rx_ring_buffer;
	else return (ptr + 1);
}

void set_mux_config(uint8_t cmd)
{
	switch(cmd)
	{
		case CMD_SET_MUX_SCI_A:
			PORTB.OUTSET = PIN_SCI_B_RX_EN; // turn off SCI B multiplexer
			PORTC.OUTSET = PIN_SCI_B_TX_EN;
			
			PORTD.OUTSET = PIN_ISO_K_EN; // turn off ISO 9141 multiplexer
		
			PORTB.OUTCLR = PIN_SCI_A_TX_EN;
			PORTC.OUTCLR = PIN_SCI_A_RX_EN;
			break;
			
		case CMD_SET_MUX_SCI_B:
			PORTB.OUTSET = PIN_SCI_A_RX_EN; // turn off SCI A multiplexer
			PORTC.OUTSET = PIN_SCI_A_TX_EN;
			
			PORTB.OUTCLR = PIN_SCI_B_TX_EN;
			PORTC.OUTCLR = PIN_SCI_B_RX_EN;
			break;
		
		case CMD_SET_MUX_9141:
			PORTB.OUTSET = PIN_SCI_A_RX_EN; // turn off SCI A multiplexer
			PORTC.OUTSET = PIN_SCI_A_TX_EN;
			
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
	//board_init();
	
	PORTB.DIRSET = PIN_SCI_A_TX_EN | PIN_SCI_B_TX_EN;
	PORTB.OUTCLR = PIN_SCI_A_TX_EN | PIN_SCI_B_TX_EN;
	
	PORTC.DIRSET = PIN_SCI_A_RX_EN | PIN_SCI_B_RX_EN;
	PORTC.OUTCLR = PIN_SCI_A_RX_EN | PIN_SCI_B_RX_EN;
	
	PORTD.DIRSET = PIN_ISO_K_EN;
	PORTD.OUTSET = PIN_ISO_K_EN;
	
	PORTR.DIRSET |= PIN0_bm | PIN1_bm; // Power LED, Data LED
	PORTR.OUTSET |= PIN0_bm; // Power LED ON
	
	// globally enable low-level interrupts in the PMIC.
	PMIC.CTRL = PMIC_LOLVLEN_bm;
	sci_setup();
	j1850vpw_setup();
	iso9141_setup();
	ccd_setup();
	usart_setup(&UART_BT, 115200);
	sei();
	set_mux_config(CMD_SET_MUX_SCI_A); // reset all enable pins to default values
	struct byte_buffer pendingTxJ1850, pendingTxIso9141, pendingTxCcd, pendingTxSci;
	pendingTxJ1850.idxLast = pendingTxIso9141.idxLast = pendingTxCcd.idxLast = pendingTxSci.idxLast = 0;
	struct byte_buffer pendingTxBt, pendingRxBt;
	pendingTxBt.idxLast = pendingTxBt.idxCurr = pendingRxBt.idxLast = 0;
	while(1)
	{
		j1850vpw_do_tasks(&pendingTxJ1850);
		iso9141_do_tasks(&pendingTxIso9141);
		ccd_do_tasks(&pendingTxCcd);
		sci_do_tasks(&pendingTxSci);
		
		if(pendingRxBt.idxLast != 0)
		{
			if(pendingRxBt.bytes[pendingRxBt.idxLast] == '\n')
			{
				if(pendingRxBt.idxLast > 3 && 
					pendingRxBt.bytes[0] == '+' && 
					pendingRxBt.bytes[1] == 'B' && 
					pendingRxBt.idxLast == (pendingRxBt.bytes[2] + 2))
				{
					if(pendingRxBt.bytes[3] == 0x80) // message
					{
						if(pendingRxBt.bytes[4] == PAYLOAD_PROTOCOL_J1850)
						{
							bt_setup_tx_buffer(&pendingRxBt, &pendingTxJ1850);
						}
						else if(pendingRxBt.bytes[4] == PAYLOAD_PROTOCOL_ISO9141)
						{
							bt_setup_tx_buffer(&pendingRxBt, &pendingTxIso9141);
						}
						else if(pendingRxBt.bytes[4] == PAYLOAD_PROTOCOL_CCD)
						{
							bt_setup_tx_buffer(&pendingRxBt, &pendingTxCcd);
						}
						else if(pendingRxBt.bytes[4] == PAYLOAD_PROTOCOL_SCI)
						{
							bt_setup_tx_buffer(&pendingRxBt, &pendingTxSci);
						}
					}
					else
					{
						// 1 byte command
						switch(pendingRxBt.bytes[3])
						{
						case CMD_SET_MUX_SCI_A:
						case CMD_SET_MUX_SCI_B:
						case CMD_SET_MUX_9141:
							set_mux_config(pendingRxBt.bytes[3]);
							bt_queue_cmd(RC_SUCCESS);
						break;
						case CMD_SET_SCI_HISPEED:
							sci_hi_speed();
							bt_queue_cmd(RC_SUCCESS);
						break;
						case CMD_SET_SCI_LOSPEED:
							sci_lo_speed();
							bt_queue_cmd(RC_SUCCESS);
						break;
						case CMD_ISO9141_5BAUDINIT:
							iso9141_start_five_baud_init();
						break;
						case CMD_ISO9141_FASTINIT:
							// TO DO
						break;
						}
					}
				}
				pendingRxBt.idxLast = 0;
			}
		}
		if(bt_rx_begin != bt_rx_last && UART_BT.STATUS & USART_DREIF_bm)
		{
			UART_BT.DATA = *bt_rx_begin;
			// set idxLast to zero, this indicates buffer empty
			bt_rx_begin = bt_rx_get_next(bt_rx_begin);
			bt_rx_buffer_fill_size--;
		}
		if(UART_BT.STATUS & USART_RXCIF_bm)
		{
			pendingRxBt.bytes[pendingRxBt.idxLast] = UART_BT.DATA;
			pendingRxBt.idxLast++;
		}
		sleepmgr_enter_sleep();
	}
}

void bt_setup_tx_buffer(struct byte_buffer *btBuffer, struct byte_buffer *txBuffer)
{
	memcpy(txBuffer->bytes, btBuffer->bytes + 2, btBuffer->idxLast - 2);
	txBuffer->idxLast = btBuffer->idxLast - 2;
	txBuffer->idxCurr = 0;
}

void bt_write_byte(uint8_t by)
{
	*bt_rx_last = by;
	bt_rx_last = bt_rx_get_next(bt_rx_last);
}

void bt_queue_cmd(uint8_t cmd)
{
	if((bt_rx_buffer_fill_size + 1) > 1024)
	{
		// buffer overflow
		bt_rx_begin = bt_rx_last;
		bt_rx_buffer_fill_size = 0;
	}
	bt_write_byte('+');
	bt_write_byte('B');
	bt_write_byte(1); // length
	bt_write_byte(cmd);
	bt_write_byte('\n');
}

void bt_queue_rx(uint8_t *srcBuffer, uint8_t srcBufferLen, uint8_t protocol)
{
	// set highest bit to indicate payload message, lower 7 bits is message length
	if(srcBufferLen + bt_rx_buffer_fill_size > 1024)
	{
		// buffer overflow
		bt_rx_begin = bt_rx_last;
		bt_rx_buffer_fill_size = 0;
	}
	bt_write_byte('+');
	bt_write_byte('B');
	bt_write_byte(0x80); // message type 80 - message
	bt_write_byte(protocol);
	bt_rx_last = bt_rx_get_next(bt_rx_last);
	for (uint8_t index = 0; index < srcBufferLen; index++)
	{
		bt_write_byte(srcBuffer[index]);
	}
	bt_write_byte('\n');
	bt_rx_buffer_fill_size += (srcBufferLen * 2) + 6;
}

ISR(USARTD1_RXC_vect)
{
	USARTD1.STATUS |= USART_RXCIF_bm;
}