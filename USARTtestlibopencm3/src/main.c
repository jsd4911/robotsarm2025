#include <libopencm3/stm32/rcc.h>
#include <libopencm3/stm32/gpio.h>
#include <libopencm3/stm32/usart.h>

#define TARGET_STR "0001"
#define TARGET_LEN 4

static void clock_setup(void) {
    rcc_periph_clock_enable(RCC_GPIOA);   // PA2/PA3 (USART2), PA5 (LD2)
    rcc_periph_clock_enable(RCC_USART2);  // 只用 USART2
}

static void led_setup(void) {
    // Nucleo-F446RE 的 LD2 = PA5
    gpio_mode_setup(GPIOA, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO5);
    gpio_clear(GPIOA, GPIO5); // 先關燈
}

static void usart2_setup(void) {
    // PA2=TX, PA3=RX, AF7
    gpio_mode_setup(GPIOA, GPIO_MODE_AF, GPIO_PUPD_NONE, GPIO2 | GPIO3);
    gpio_set_af(GPIOA, GPIO_AF7, GPIO2 | GPIO3);

    usart_set_baudrate(USART2, 115200);
    usart_set_databits(USART2, 8);
    usart_set_stopbits(USART2, USART_STOPBITS_1);
    usart_set_parity(USART2, USART_PARITY_NONE);
    usart_set_flow_control(USART2, USART_FLOWCONTROL_NONE);
    usart_set_mode(USART2, USART_MODE_TX_RX);
    usart_enable(USART2);
}

static inline void pc_putc(char c) { usart_send_blocking(USART2, (uint16_t)c); }
static inline int  rx_ready(void)  { return usart_get_flag(USART2, USART_SR_RXNE); } // F4 用 SR

int main(void) {
    clock_setup();
    led_setup();
    usart2_setup();

    // 前綴匹配狀態機：連續收到 "0001" -> 亮 LD2
    int k = 0;

    while (1) {
        if (rx_ready()) {
            char c = (char)usart_recv(USART2);

            // 回顯到 PC（WinForms 或 PIO Monitor 會看到你打的字）
            pc_putc(c);

            // 匹配 "0001"
            if (c == TARGET_STR[k]) {
                if (++k == TARGET_LEN) {
                    gpio_set(GPIOA, GPIO5); // 命中 -> LD2 ON
                    k = 0;                  // 繼續偵測下一次（需要保留就移除這行）
                }
            } else {
                // 失配：若當前字元等於首字元，k=1，否則歸零
                k = (c == TARGET_STR[0]) ? 1 : 0;
            }
        }
    }
    return 0;
}

