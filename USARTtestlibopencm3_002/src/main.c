#include <libopencm3/stm32/rcc.h>
#include <libopencm3/stm32/gpio.h>
#include <libopencm3/stm32/usart.h>
#include <stdio.h>

static void clock_setup(void) {
    // 84MHz：用 HSI → PLL，把系統時脈設為 84MHz，確保鮑率計算正確
    rcc_clock_setup_pll(&rcc_hsi_configs[RCC_CLOCK_3V3_84MHZ]);
}

static void usart2_setup(void){
    rcc_periph_clock_enable(RCC_GPIOA);// 開 GPIOA 時鐘（PA2/PA3 用）
    rcc_periph_clock_enable(RCC_USART2);// 開 USART2 外設時鐘

    // PA2=TX, PA3=RX → AF7，把 PA2/PA3 設成替代功能（AF），不拉上下拉
    gpio_mode_setup(GPIOA, GPIO_MODE_AF, GPIO_PUPD_NONE, GPIO2 | GPIO3);
    // 指定 AF7（對應 USART2 功能）
    gpio_set_af(GPIOA, GPIO_AF7, GPIO2 | GPIO3);

    usart_set_baudrate(USART2, 115200);// 設定鮑率 115200
    usart_set_databits(USART2, 8);// 8 資料位
    usart_set_stopbits(USART2, USART_STOPBITS_1);// 1 停止位
    usart_set_parity(USART2, USART_PARITY_NONE);// 無同位
    usart_set_flow_control(USART2, USART_FLOWCONTROL_NONE);// 無流控
    usart_set_mode(USART2, USART_MODE_TX_RX);   // 同時收發
    usart_enable(USART2);// 啟用 USART2
}

static inline void u2_putc(char c){ usart_send_blocking(USART2, (uint16_t)c); }// 傳一個字元（阻塞直到硬體送出）
static void u2_puts(const char*s){ while(*s) u2_putc(*s++); }// 逐字送出字串直到 '\0'

int main(void){
    clock_setup();// ★ 先設系統時脈（避免鮑率飄掉）
    usart2_setup();// 初始化 USART2（PA2/PA3）

    u2_puts("\r\n[STM32 READY] Send text, press Enter.\r\n");// 上電提示字串（CRLF 結尾）

    // // 行緩衝（最多 127 字元），idx 指目前長度
    char line[128]; int idx = 0;

    while (1){ // 主迴圈
        if (usart_get_flag(USART2, USART_SR_RXNE)) {     // 有接收資料（F4 用 SR_RXNE）
            char c = (char)usart_recv(USART2);// 讀一個字元（同時清 RXNE）

            
            u2_putc(c);// 立刻回顯該字元到 PC（讓使用者看到自己打的）

            
            if (c == '\r' || c == '\n') {   // 若收到 CR 或 LF，視為一行結束
                if (idx > 0) {  // 緩衝至少有內容才回覆
                    line[idx] = 0; // 串尾補 '\0'，形成 C 字串
                    u2_puts("\r\n[ACK] ");// 回傳 ACK 前綴（另起一行）
                    u2_puts(line);// 回傳剛剛收集到的那一行內容
                    u2_puts("\r\n");                  // CRLF 結尾，配合 PC 的 ReadLine()
                    idx = 0;// 清空緩衝，準備下一行
                }
            } else {
                if (idx < (int)sizeof(line)-1) // 還有空間
                line[idx++] = c;// 累積到行緩衝
                // 若滿了就自動丟棄後續字元（避免溢位）
            }
        }
    }
}
