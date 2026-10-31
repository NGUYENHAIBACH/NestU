# Mô hình thương mại hoá --- Nền tảng quản lý & cho thuê nhà trọ

Oct 29, 2026 · Chuẩn bị bởi \@Bách Nguyễn Hải

> Tài liệu này diễn giải lại đặc tả kỹ thuật (BHMS + BHRP) thành một
> **mô hình thương mại hoá**: ai trả tiền, trả cho cái gì, bao nhiêu, và
> vì sao --- căn cứ trực tiếp vào các cơ chế đã có trong spec (tier
> Free/Plus/Pro, SubscriptionPlan, PostPurchase, Deposit, PostReach),
> không phải ý tưởng mới ngoài schema.

## 1. Tổng quan mô hình

Hệ thống gồm 2 mảng ghép lại thành một vòng giá trị:

-   **BHMS (Boarding House Management System)** --- phần mềm vận hành
    cho chủ nhà trọ (SaaS B2B nhỏ): quản lý phòng, hợp đồng, hoá đơn,
    nhân sự.

-   **BHRP (Boarding House Rental Platform)** --- chợ cho thuê hai chiều
    (marketplace): người đăng tin (môi giới/chủ nhà) ↔ người tìm thuê.

Điểm thương mại mấu chốt: **một chủ nhà trọ vừa là khách hàng SaaS (trả
phí theo tier để vận hành) vừa là người bán trên marketplace (trả phí
theo tin đăng/lượt xem)**. Hai dòng tiền này tách biệt về cơ chế nhưng
cùng một khách hàng --- đây là đòn bẩy bán chéo (cross-sell) chính của
mô hình:

  ------------------------------------------------------------------------
                    BHMS (vận hành)          BHRP (marketplace)
  ----------------- ------------------------ -----------------------------
  Ai trả            Chủ nhà trọ (landlord)   Chủ nhà trọ + môi giới
                                             (leasing agent)

  Trả cho cái gì    Hạn mức phòng, tính năng Lượt đăng tin, lượt xem/phân
                    vận hành (nhân sự, báo   tích
                    cáo, AI)                 

  Cơ chế giá        Subscription theo tier   Pay-per-use (credit theo tin
                    (Free/Plus/Pro)          đăng) + add-on

  Vai trò trong     Giữ chân (retention),    Lấp đầy phòng trống → nguồn
  funnel            LTV dài hạn              thu thu hẹp theo nhu cầu thực
                                             tế
  ------------------------------------------------------------------------

## 2. Phân khúc khách hàng

  --------------------------------------------------------------------------
  Phân khúc    Vai trò trong hệ Nhu cầu chính       Sẵn sàng chi trả
               thống                                
  ------------ ---------------- ------------------- ------------------------
  Chủ nhà trọ  Landlord,        Thu tiền đúng hạn,  Thấp, nhạy giá --- Free
  nhỏ (1 cơ    Free/Plus        nhắc hoá đơn, đọc   phải đủ dùng thật, không
  sở, dưới                      công tơ đơn giản    phải demo cụt
  \~20 phòng)                                       

  Chủ nhà trọ  Landlord, Pro    Quản lý nhân sự/ca  Cao --- trả theo giá trị
  chuỗi (nhiều                  trực, báo cáo đa cơ vận hành tiết kiệm được,
  cơ sở, có                     sở, chuyển đổi      không theo số phòng
  nhân viên)                    context nhiều       
                                property            

  Môi giới /   Không sở hữu     Đăng nhiều tin,     Trung bình --- trả theo
  leasing      BoardingHouse,   tiếp cận nhiều      số lượt đăng/hiệu quả,
  agent        chỉ đăng tin     khách, không cần    không theo subscription
                                công cụ vận hành    

  Người tìm    Khách vãng lai   Tìm phòng nhanh,    Rất thấp --- là "hàng
  thuê         trên marketplace chạy chật cọc giợ   hoá" tạo thanh khoản cho
  (platform                                         phía bán, không phải
  user)                                             nguồn thu trực tiếp

  Tenant đang  Người dùng cuối  Thanh toán, xem hoá Không trả trực tiếp ---
  thuê         của chủ nhà trọ  đơn, khiếu nại      chi phí nằm trong giá
                                                    trọ mà chủ nhà đã trả
  --------------------------------------------------------------------------

Hai phân khúc trả tiền thật là **chủ nhà trọ** và **môi giới**; tenant
và người tìm thuê là đầu vào tạo giá trị (dể dùng, thanh khoản) giúp giự
chân hai phân khúc trả tiền đó.

## 3. Các dòng doanh thu

Bốn dòng doanh thu, đều đã có điểm tữa trong schema/spec hiện tại ---
không cần tính năng mới để thu đầu tiên.

### 3.1. Subscription vận hành (BHMS) --- doanh thu đều, gụi tự (recurring)

UserSubscription theo 3 tier **Free / Plus / Pro**, giới hạn bởi
SubscriptionPlan.maxRoom (số phòng) và bộ tính năng mở dần:

-   **Free** --- vận hành cơ bản: thiết lập property, phòng, hợp đồng,
    thu QR, lịch sử thanh toán, ghi chỉ số thủ công, chat. Đủ dùng thật
    cho 1 nhà trọ nhỏ --- đây là kênh thu hút (acquisition), không phải
    bản demo cụt.

-   **Plus** --- thêm gợi AI soạn tin đăng, broadcast thông báo, quản lý
    cọc/nợ/chi phí/dịch vụ riêng, xuất hợp đồng, quản lý tài sản (Asset)
    --- nhóm tính năng giảm công việc thủ công hàng ngày.

-   **Pro** --- thêm quản lý nhân sự, lịch ca, chấm công, chuyển đổi đa
    property, báo cáo tổng hợp nhiều cơ sở --- dành cho chủ trọ chuỗi có
    nhân viên.

Đây là **doanh thu lõi, kiếm định được**: gợi theo tier × số property,
dễ dự báo MRR.

### 3.2. Bán lượt đăng tin (BHRP) --- pay-per-use, thu hút cả người không dùng BHMS

Mọi user đăng tin đều có hạn mức miễn phí/ngày
(BASE_DAILY_FREE_POST_QUOTA = 3), cộng thêm bó theo tier nếu là landlord
đang trả subscription (Free: +0 → 3/ngày, Plus: +5 → 8/ngày, Pro: +10 →
13/ngày). Vượt hạn mức → mua PostPurchase (gói credit đăng tin), dùng
chung cơ chế cho cả landlord và môi giới độc lập (không cần
subscription).

đây là đòn bẩy quan trọng: **môi giới không sở hữu nhà trọ vẫn là khách
hàng trả tiền** qua kênh này --- mở rộng thị trường ra ngoài tập chủ trọ
đang dùng phần mềm quản lý. Động lực bán thêm subscription cho landlord
cũng nằm ngay ở đây: nâng tier → nhiều lượt đăng miễn phí hơn → tiết
kiệm tiền mua credit.

### 3.3. Analytics add-on (Post Reach) --- upsell trên hành vi đã có

PostReach (lượt xem, xu hướng theo ngày) được spec đánh dấu là **paid
add-on độc lập với cả subscription và credit đăng tin** --- nhằm để ngỏ
cho cả môi giới không có subscription cũng mua được. Ba cách thương mại
hoá khả dĩ (cần quyết định sản phẩm, spec đã flag):

1.  **Bundle miễn phí kèm PostPurchase** --- dễ bán nhất, nhưng làm giảm
    giá trị của mục riêng.

2.  **Gói riêng** (AnalyticsAddon mới) --- bán định kỳ hoặc one-time,
    tách doanh thu rõ ràng --- **đề xuất chọn hướng này** vì giữ được
    biên lợi trên một tính năng data đạt giá trị cao với người đăng tin
    chuyên nghiệp.

3.  Khoá cứng vào Pro tier --- đẩy môi giới ra ngoài tính năng, không
    khuyến nghị.

### 3.4. Dòng tiền giao dịch trên marketplace (đề xuất mở rộng)

Spec hiện tại xử lý Deposit (cọc giợ qua nền tảng) như một bước chuyển
tiếp (pass-through) sang hợp đồng, **chưa có phí hoặc hoa hồng nền tảng
trên giao dịch này**. Đây là khoảng trống doanh thu lớn nhất nếu muốn
thương mại hoá sâu hơn:

-   **Phí giao dịch cọc** (vd. 1--2% trên Deposit.amount, trừ khi hoàn
    cọc) --- gần với mô hình marketplace cổ điển.

-   **Phí thành công khi cọc chuyển thành hợp đồng** (UC-L-04b) --- chỉ
    thu khi nền tảng tạo ra kết quả thật, dễ bán hơn về mặt cảm nhận.

Cần thêm field/model mới (vd. platformFeeAmount trên Payment hoặc
Deposit) --- đánh dấu đây là đề xuất nạm ngoài spec kỹ thuật hiện tại,
không phải mô tả lại nó.

## 4. Cấu trúc giá đề xuất

Giá minh họa, cần khảo sát thị trường để chốt số --- nhưng lớp cấu trúc
bám đúng biên giới tính năng đã có trong spec (SubscriptionPlan.maxRoom,
dailyPostQuote).

  -------------------------------------------------------------------------
                            **Free**        **Plus**        **Pro**
  ------------------------- --------------- --------------- ---------------
  Giá/tháng                 0đ              \~149.000đ      \~399.000đ

  Số phòng tối đa           Thấp (vd. ≤ 10) Trung (vd. ≤    Không giới hạn
                                            50)             

  Lượt đăng tin miễn        3               8               13
  phí/ngày                                                  

  AI gợi ý tin đăng,        ---             ✓               ✓
  broadcast, quản lý                                        
  cọc/chi phí/dịch vụ,                                      
  asset                                                     

  Quản lý nhân sự, ca, chấm ---             ---             ✓
  công, đa property, báo                                    
  cáo tổng hợp                                              
  -------------------------------------------------------------------------

**Bán thêm theo nhu cầu (không phụ thuỌ tier):**

  -----------------------------------------------------------------------
  Sản phẩm              Hình thức                       Giá minh họa
  --------------------- ------------------------------- -----------------
  Gói credit đăng tin   Mua theo số lượng, dùng dần     vd. 10 tin /
  (PostPurchase)        (FIFO)                          50.000đ, 50 tin /
                                                        199.000đ

  Post Reach Analytics  Gói tháng hoặc one-time, độc    vd. 29.000đ/tháng
                        lập với tier                    

  Phí giao dịch cọc (đề \% trên Deposit.amount khi      vd. 1--2%
  xuất)                 chuyển thành hợp đồng           
  -----------------------------------------------------------------------

**Nguyên tắc định giá:**

-   Free phải thật sự dùng được cho 1 nhà trọ nhỏ --- đây là kênh thu
    hút rẻ nhất, không phải bản dùng thử có hạn ngày.

-   Giá trị nâng tier nằm ở **thời gian vận hành tiết kiệm được**
    (Plus/Pro), không chỉ ở số phòng --- nên định vị giá theo giá trị
    này khi bán, không chỉ theo cấu hình.

-   Cơ chế credit đăng tin dùng chung cho landlord và môi giới --- giúp
    giá niêm yết đơn giản, không cần hai bảng giá tách biệt cho hai vai
    trò đăng tin.

## 5. Đơn vị kinh tế & đòn bẩy tăng trưởng

### Vòng phát triển (growth loop)

Post (BHRP) là kênh **acquisition không tốn phí marketing**: một chủ nhà
trọ đăng tin để lấp phòng trống → nhìn thấy công cụ quản lý BHMS ngay
trong cùng sản phẩm → chuyển sang dùng Free BHMS → nâng Plus/Pro khi số
phòng/nhân sự tăng. Riêng kênh đăng tin cũng tự thu hút môi giới độc lập
không cần qua BHMS.

### Đòn bẩy nâng tier

-   **Số phòng chạm ngưỡng maxRoom** --- điểm nâng cấp tự nhiên, không
    cần thuyết phục.

-   **Hết quota đăng tin miễn phí liên tục nhiều ngày** → gợi ý nâng
    tier (rẻ hơn mua credit lẻ dài hạn) thay vì chỉ bán credit.

-   **Tuyển nhân viên đầu tiên** → điểm chuyển Plus → Pro rõ nhất, vì
    Pro là tier duy nhất mở EmployeeAssignment/lịch ca/chấm công.

### Rủi ro cầu trúc doanh thu

-   Doanh thu credit đăng tin (3.2) phụ thuềc chu kỳ lấp phòng trống của
    thị trường --- biểu đồng theo mùa, không ổn định như subscription.
    Nên xem đây là doanh thu **bổ sung**, không phải trụ cột.

-   Phí giao dịch cọc (3.4) đang là đề xuất chưa có trong schema --- nếu
    triển khai sẽ là nguồn doanh thu có biên lợi cao nhất vì không tốn
    chi phí vận hành thêm, nhưng cần cân nhắc tác động lên tỉ lệ chuyển
    đổi cọc → hợp đồng (phí cao quá có thể đẩy giao dịch ra ngoài nền
    tảng).

## 6. Rủi ro & khuyến nghị tiếp theo

-   [ ] Chốt hướng thương mại hoá Post Reach Analytics (3.3) --- bundle,
    gói riêng, hay khoá Pro --- cần quyết định sản phẩm trước khi có
    model AnalyticsAddon.

-   [ ] Quyết định có thêm phí giao dịch cọc trên marketplace hay không
    (3.4) --- đây là khoảng trống doanh thu lớn nhất nhưng định khiến
    môi giới/chủ trọ tìm cách giao dịch ngoài nền tảng nếu định giá sai.

-   [ ] Kiểm định giá trần/sàn thật (vịng market test) trước khi chốt số
    ở bảng giá mục 4 --- đây chỉ là giá minh họa theo cấu trúc tier đã
    có.

-   [ ] Tách theo dõi MRR (subscription) khỏi doanh thu giao dịch
    (credit đăng tin, cọc) trong báo cáo tài chính nội bộ --- hai loại
    biến động khác nhau, gộp chung sẽ che mất tín hiệu retention thật.

-   [ ] Xác nhận Expense.roomId bắt buộc (gap đã flag trong spec) không
    ảnh hưởng để khoản chi phí dùng chung --- nếu landlord Pl + không
    ghi được chi phí chung, tính năng Expense Management sẽ giảm giá trị
    bán hàng thật tế.
