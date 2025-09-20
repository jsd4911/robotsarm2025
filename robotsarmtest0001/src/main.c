/**
 * @file   main.c
 * @brief  Blinking LED example for STM32 Nucleo-F446RE.
 */

#include <libopencm3/stm32/rcc.h>
#include <libopencm3/stm32/gpio.h>

/* User LED (LD2) connected to Arduino-D13 pin. */
#define RCC_MotoCH_GPIO (RCC_GPIOA)
#define GPIO_MotoOne_PORT (GPIOA)
#define GPIO_MotoOne_PIN (GPIO5)
#define GPIO_MotoOneDIR_PORT (GPIOA)
#define GPIO_MotoOneDIR_PIN (GPIO9)

int n = 1500;
int t = 100;


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
  rcc_periph_clock_enable(RCC_MotoCH_GPIO);

  /* Set LED pin to output push-pull. */
  gpio_mode_setup(GPIO_MotoOne_PORT,
                  GPIO_MODE_OUTPUT,
                  GPIO_PUPD_NONE,
                  GPIO_MotoOne_PIN);

  gpio_set_output_options(GPIO_MotoOne_PORT,
                          GPIO_OTYPE_PP,
                          GPIO_OSPEED_2MHZ,
                          GPIO_MotoOne_PIN);

  gpio_mode_setup(GPIO_MotoOneDIR_PORT,
                  GPIO_MODE_OUTPUT,
                  GPIO_PUPD_NONE,
                  GPIO_MotoOneDIR_PIN);

  gpio_set_output_options(GPIO_MotoOneDIR_PORT,
                          GPIO_OTYPE_PP,
                          GPIO_OSPEED_2MHZ,
                          GPIO_MotoOneDIR_PIN);

  /* Start blinking. */
  while (1)
  {
    t--;
    int s = 10000;
    if(n == 0)
    {
      n = 1500;
      gpio_toggle(GPIO_MotoOneDIR_PORT, GPIO_MotoOneDIR_PIN); /* Direction pin toggle. */
    }
    if(t <= 0)
    {
        s = 2000;
    }
    gpio_toggle(GPIO_MotoOne_PORT, GPIO_MotoOne_PIN); /* LED on/off. */
    delay(s);
    n--;
  }

  return 0;
}