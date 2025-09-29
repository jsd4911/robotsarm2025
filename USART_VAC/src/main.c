#include <libopencm3/stm32/rcc.h> // RCC：開週邊時鐘用
#include <libopencm3/stm32/gpio.h> // GPIO：腳位模式/輸出入
#include <libopencm3/stm32/usart.h> // USART：序列埠設定/收送
#include <libopencm3/stm32/adc.h> // ADC：類比數位轉換器
#include <stdio.h>                    // sprintf() 需要的標頭

#define VREF   3.3f          // 參考電壓（假設 VDDA≈3.3V；要更準可做 Vref 校正）
#define CH     0             // 要量的 ADC 通道：ADC1_IN0 = PA0
#define BAUD   115200        // 串口鮑率

static void clock_setup(void) {
    rcc_clock_setup_pll(&rcc_hsi_configs[RCC_CLOCK_3V3_84MHZ]);
}

/* ---- 簡單 busy-wait 延遲（粗略毫秒） ---- */
static void delay(volatile uint32_t t){
    while(t--) __asm__("nop"); // 每次迴圈做一次空指令，單純拖時間
}

/* ---- USART2: PA2/PA3 ---- */
static void usart2_setup(void){
    rcc_periph_clock_enable(RCC_GPIOA); // 開 GPIOA 時鐘（PA2/PA3）
    rcc_periph_clock_enable(RCC_USART2); // 開 USART2 時鐘

    // 將 PA2/PA3 設為 AF 模式，配 AF7（對應 USART2 TX/RX）
    gpio_mode_setup(GPIOA, GPIO_MODE_AF, GPIO_PUPD_NONE, GPIO2 | GPIO3);
    gpio_set_af(GPIOA, GPIO_AF7, GPIO2 | GPIO3);

    // 基本串口參數：115200, 8N1, 無流控
    usart_set_baudrate(USART2, BAUD);
    usart_set_databits(USART2, 8);
    usart_set_stopbits(USART2, USART_STOPBITS_1);
    usart_set_parity(USART2, USART_PARITY_NONE);
    usart_set_flow_control(USART2, USART_FLOWCONTROL_NONE);
    usart_set_mode(USART2, USART_MODE_TX); // 只開 TX（只需要印資料）
    usart_enable(USART2);                  // 使能 USART2
}

static inline void putc_usart(char c){ usart_send_blocking(USART2, (uint16_t)c); } // 送一個字元（阻塞直到發完）
static void puts_usart(const char *s){ while(*s) putc_usart(*s++); } // 送字串（逐字送出）

/* ---- LD2: PA5 （用來做簡單指示） ---- */
static void led_setup(void){
    rcc_periph_clock_enable(RCC_GPIOA); // 開 GPIOA 時鐘（PA5 在 A 組）
    gpio_mode_setup(GPIOA, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO5); // PA5 設為推挽輸出
    gpio_clear(GPIOA, GPIO5); // 先關燈（輸出低）
}

/* ---- ADC1 單通道、單次轉換，PA0 ---- */
static void adc1_setup_single_on_pa0(void){
    rcc_periph_clock_enable(RCC_GPIOA); // PA0 腳位時鐘
    rcc_periph_clock_enable(RCC_ADC1); // ADC1 週邊時鐘

    /* PA0 設為 Analog */
    gpio_mode_setup(GPIOA, GPIO_MODE_ANALOG, GPIO_PUPD_NONE, GPIO0); // PA0 切成類比模式（ADC 專用）

    /* ADC 時鐘預分頻（F4: 由 APB2 來，這裡設 /4 較保守） */
    adc_set_clk_prescale(ADC_CCR_ADCPRE_BY4); // ADC 時鐘預分頻（/4，較保守，也利於精準）

    /* 解析度 12-bit、右對齊、單次轉換、非掃描 */
    adc_power_off(ADC1); // 先關掉 ADC 才改設定（避免轉換中改參數）
    adc_disable_scan_mode(ADC1); // 不掃描多通道（單通道）
    adc_set_single_conversion_mode(ADC1); // 單次轉換模式（每次手動觸發一次）
    adc_set_right_aligned(ADC1); // 右對齊（12-bit 會在低位）
    adc_set_resolution(ADC1, ADC_CR1_RES_12BIT); // 解析度 12-bit（0..4095）

    /* 取樣時間要看訊號源阻抗；>10kΩ 建議拉長。這裡用最長 480 cycles，穩一點 */
    adc_set_sample_time_on_all_channels(ADC1, ADC_SMPR_SMP_480CYC); // 取樣時間 480 cycles（高阻抗訊號較穩）

    /* 設定規則序列只轉一個通道：CH=0 (PA0) */
    uint8_t channels[1] = { CH }; // 轉換序列：只放一個通道（這裡是 IN0/PA0）
    adc_set_regular_sequence(ADC1, 1, channels); // 設定「規則序列」長度=1，內容=CH

    /* 啟動 ADC */
    adc_power_on(ADC1); // 開啟 ADC
    /* 上電後稍等一下（datasheet 建議 > 10us），這裡粗略延遲 */
    delay(10000); // 上電後稍等（>10us），確保穩定
}

/* 做一次單次轉換並回傳 0..4095 */
static uint16_t adc1_read_once(void){ // 做一次單次轉換並回傳 0..4095 原始值
    adc_start_conversion_regular(ADC1); // 觸發「規則序列」轉換
    while(!adc_eoc(ADC1));             // 等待 EOC（End Of Conversion）旗標
    return adc_read_regular(ADC1);  // 讀出轉換結果（同時清 EOC）
}

static void print_u16_hex(uint16_t v){  // 把 16-bit 整數印成 0xXXXX
    const char *hex = "0123456789ABCDEF";
    char buf[7] = "0x0000";
    buf[2] = hex[(v >> 12) & 0xF];
    buf[3] = hex[(v >> 8)  & 0xF];
    buf[4] = hex[(v >> 4)  & 0xF];
    buf[5] = hex[(v)       & 0xF];
    puts_usart(buf);
}

int main(void){ 
    clock_setup();        // ★ 一定要放最前面
    usart2_setup(); // 初始化 USART2（印資料到 PC）
    led_setup(); // 初始化 LED（PA5）
    adc1_setup_single_on_pa0(); // 初始化 ADC1（單通道 PA0）

    puts_usart("\r\n[ADC DEMO] ADC1@PA0, 12-bit, VREF=3.3V\r\n"); // 開機訊息

    while(1){
        /* 取 N 次平均（簡單抗雜訊） */
        const int N = 8; // 做 N 次平均（簡單降噪）
        uint32_t acc = 0; // 累加器
        for(int i=0;i<N;i++){ acc += adc1_read_once(); } // 連續量 N 次並累加
        uint16_t raw = (uint16_t)(acc / N); // 求平均後的 12-bit 原始值

        /* 換算電壓：raw in [0..4095] -> Volt */
        float volt = (raw / 4095.0f) * VREF;

        /* 小輸出：RAW + 電壓值 */
        puts_usart("RAW="); // 印「RAW=」
        print_u16_hex(raw); // 以 0xXXXX 格式印原始值
        puts_usart("  V="); // 印空格與「V=」
        /* 粗略把 float 印成字串（避免引 libm），這裡手動到 2 位小數 */
        int mv = (int)(volt * 1000.0f + 0.5f); // 轉毫伏、四捨五入到整數 mV
        int v_int = mv / 1000; // 取整數伏特
        int v_dec = (mv % 1000) / 10;         // 兩位小數
        char line[46]; // 取小數兩位（xx.xx V）, 暫存要印的字串
        /* v_int.dec(2位) */
        int n = 0; // 累積寫入長度（可不使用）
        n += sprintf(line + n, "%d.", v_int); // 印整數部份與小數點
        n += sprintf(line + n, "%02d V", v_dec); // 印兩位小數與單位、換行
        int Voltage = ((v_int *100) + v_dec) - 60;//整數伏特放大100倍加兩位小數電壓為實際電壓放大100倍，2.8V~0.6V為180~0度，0度對應0.6V所以減60
        int A = Voltage/1.23;//電壓轉換角度公式，分母做微調
        sprintf(line + n,"  Angle = %d°\r\n", A);
        puts_usart(line); // 送到 PC 端

        /* 閃一下 LED 當心跳 */
        gpio_toggle(GPIOA, GPIO5); // LED 閃一下（心跳/取樣指示）

        /* 約 200ms（粗略） */
        delay(1200000); // 粗略延遲 ≈200ms（視時脈而定）
    }
    return 0;
}