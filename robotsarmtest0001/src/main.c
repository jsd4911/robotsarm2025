/**
 * @file   main.c
 * @brief  Blinking LED example for STM32 Nucleo-F446RE.
 */

#include <libopencm3/stm32/rcc.h>
#include <libopencm3/stm32/gpio.h>

/* User LED (LD2) connected to Arduino-D13 pin. */
#define RCC_LED_GPIO (RCC_GPIOA)
#define GPIO_LED_PORT (GPIOA)
#define GPIO_LED_PIN (GPIO5)
#define GPIO_DIR_PORT (GPIOA)
#define GPIO_DIR_PIN (GPIO9)

int n = 20000;


static void delay(uint32_t value)
{
  for (uint32_t i = 0; i < value; i++)
  {
    __asm__("nop"); /* Do nothing. */
  }
}

int main(void)
{
  /* Enable clock. */
  rcc_periph_clock_enable(RCC_LED_GPIO);

  /* Set LED pin to output push-pull. */
  gpio_mode_setup(GPIO_LED_PORT,
                  GPIO_MODE_OUTPUT,
                  GPIO_PUPD_NONE,
                  GPIO_LED_PIN);

  gpio_set_output_options(GPIO_LED_PORT,
                          GPIO_OTYPE_PP,
                          GPIO_OSPEED_2MHZ,
                          GPIO_LED_PIN);

  gpio_mode_setup(GPIO_DIR_PORT,
                  GPIO_MODE_OUTPUT,
                  GPIO_PUPD_NONE,
                  GPIO_DIR_PIN);

  gpio_set_output_options(GPIO_DIR_PORT,
                          GPIO_OTYPE_PP,
                          GPIO_OSPEED_2MHZ,
                          GPIO_DIR_PIN);

  /* Start blinking. */
  while (1)
  {
    if(n == 0)
    {
      n = 10000;
      gpio_toggle(GPIO_DIR_PORT, GPIO_DIR_PIN); /* Direction pin toggle. */
    }
    gpio_toggle(GPIO_LED_PORT, GPIO_LED_PIN); /* LED on/off. */
    delay(2000);
    n--;
  }

  return 0;
}