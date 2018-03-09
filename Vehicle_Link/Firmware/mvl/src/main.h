/*
 * main.c
 *
 * Created: 2/5/2018
 *  Author: Katie M
 */

#ifndef _MAIN_H_
#define _MAIN_H_

#include "definitions.h"
#include "asf.h"

void usart_setup(USART_t *usart,int8_t bscale, uint16_t bsel);
void set_mux_config(MUX_CONFIG_t config);
void byte_buffer_putchar(struct byte_buffer *buffer, uint8_t ch);

void j1850vpw_setup(void);
void j1850vpw_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer);

void iso9141_setup(void);
void iso9141_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer);
void iso9141_start_five_baud_init(uint8_t initByte);

void ccd_setup(void);
void ccd_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer);

void sci_setup_lo_speed(void);
void sci_setup_hi_speed(void);
void sci_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer);

#endif // _MAIN_H_
