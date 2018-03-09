/*
 * SCI.c
 *
 * Created: 3/5/2018 4:01:57 PM
 *  Author: rk5642
 */ 
#include "main.h"
#include <string.h>

#define UART_SCI		USARTE0

uint8_t recv_buff[255]; /* CCD receive buffer / MAX 8 Bytes */
int recv_buff_len = 0; /* CCD receive buffer pointer */

uint8_t send_buff[255];
int send_buff_len = 0;
int send_cur_index = -1;
bool inHighSpeedMode = false;

void sci_setup_lo_speed(void)
{
	usart_tx_disable(&UART_SCI);
	usart_rx_disable(&UART_SCI);
	usart_setup(&UART_SCI, BAUD_7812_BSCALE, BAUD_7812_BSEL);
}

void sci_setup_hi_speed(void)
{
	usart_tx_disable(&UART_SCI);
	usart_rx_disable(&UART_SCI);
	usart_setup(&UART_SCI, BAUD_62500_BSCALE, BAUD_62500_BSEL);
}

void sci_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer)
{
	if(UART_SCI.STATUS & USART_RXCIF_bm)
	{
		recv_buff[recv_buff_len] = UART_SCI.DATA;
		recv_buff_len++;
		if(send_cur_index > 0)
		{
			if(!inHighSpeedMode && send_cur_index == 1 && send_buff[0] == 0x12 && recv_buff[0] == 0x12)
			{
				sci_setup_hi_speed();
				inHighSpeedMode = true;
			}
			if(send_cur_index < send_buff_len)
			{
				usart_putchar(&UART_SCI, send_buff[send_cur_index]);
				send_cur_index++;
			}
			else
			{
				send_cur_index = -1;
				send_buff_len = 0;
				memcpy(readBuffer->bytes, recv_buff, recv_buff_len);
				readBuffer->idxCurr = 0;
				readBuffer->idxLast = recv_buff_len;
			}
		}
		else
		{
			send_cur_index = -1;
			send_buff_len = 0;
			memcpy(readBuffer->bytes, recv_buff, recv_buff_len);
			readBuffer->idxCurr = 0;
			readBuffer->idxLast = recv_buff_len;
		}
	}
	if(send_cur_index == 0)
	{
		usart_putchar(&UART_SCI, send_buff[send_cur_index]);
		send_cur_index++;
	}
	
	if(inHighSpeedMode && send_cur_index > 0 && send_buff[send_cur_index - 1] == 0xFE)
	{
		sci_setup_lo_speed();
		inHighSpeedMode = false;
	}
}