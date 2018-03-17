/**
 * \file
 *
 * \brief ATxmega32A4U on STK600 board configuration template
 *
 */
/*
 * Support and FAQ: visit <a href="http://www.atmel.com/design-support/">Atmel Support</a>
 */

#ifndef CONF_BOARD_H
#define CONF_BOARD_H

// Enable on-board AT45DBX interface (SPI)
#define CONF_BOARD_AT45DBX

// Enable UART Communication Port interface (UART)
#define CONF_BOARD_ENABLE_USARTC0
#define CONF_BOARD_ENABLE_USARTC1
#define CONF_BOARD_ENABLE_USARTD0
#define CONF_BOARD_ENABLE_USARTD1
#define CONF_BOARD_ENABLE_USARTE0

//! Definition of USART connection for this example
//! TXC0 on PC3 and RXC0 on PC2
#define  USART_RX_Vect       USARTC0_RXC_vect
#define  USART_DRE_Vect      USARTC0_DRE_vect

#endif // CONF_BOARD_H
