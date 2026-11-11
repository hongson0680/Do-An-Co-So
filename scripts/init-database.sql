-- Tạo database (bỏ qua nếu đã có)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'QuanLyTinDung')
BEGIN
    CREATE DATABASE QuanLyTinDung
    COLLATE Vietnamese_CI_AS;
END
GO

USE QuanLyTinDung;
GO

-- ============================================================
-- PHẦN 1: XÓA CÁC BẢNG CŨ (nếu tồn tại, theo thứ tự phụ thuộc)
-- ============================================================
IF OBJECT_ID('dbo.nhat_ky_he_thong',    'U') IS NOT NULL DROP TABLE dbo.nhat_ky_he_thong;
IF OBJECT_ID('dbo.thong_bao',           'U') IS NOT NULL DROP TABLE dbo.thong_bao;
IF OBJECT_ID('dbo.thanh_toan',          'U') IS NOT NULL DROP TABLE dbo.thanh_toan;
IF OBJECT_ID('dbo.lich_tra_no',         'U') IS NOT NULL DROP TABLE dbo.lich_tra_no;
IF OBJECT_ID('dbo.hop_dong_vay',        'U') IS NOT NULL DROP TABLE dbo.hop_dong_vay;
IF OBJECT_ID('dbo.phe_duyet',           'U') IS NOT NULL DROP TABLE dbo.phe_duyet;
IF OBJECT_ID('dbo.tham_dinh_tin_dung',  'U') IS NOT NULL DROP TABLE dbo.tham_dinh_tin_dung;
IF OBJECT_ID('dbo.tai_san_dam_bao',     'U') IS NOT NULL DROP TABLE dbo.tai_san_dam_bao;
IF OBJECT_ID('dbo.tai_lieu_vay',        'U') IS NOT NULL DROP TABLE dbo.tai_lieu_vay;
IF OBJECT_ID('dbo.ho_so_vay',           'U') IS NOT NULL DROP TABLE dbo.ho_so_vay;
IF OBJECT_ID('dbo.nguoi_dung',          'U') IS NOT NULL DROP TABLE dbo.nguoi_dung;
IF OBJECT_ID('dbo.san_pham_vay',        'U') IS NOT NULL DROP TABLE dbo.san_pham_vay;
IF OBJECT_ID('dbo.khach_hang',          'U') IS NOT NULL DROP TABLE dbo.khach_hang;
IF OBJECT_ID('dbo.chi_nhanh',           'U') IS NOT NULL DROP TABLE dbo.chi_nhanh;
IF OBJECT_ID('dbo.vai_tro',             'U') IS NOT NULL DROP TABLE dbo.vai_tro;
GO

-- ============================================================
-- PHẦN 2: BẢNG DANH MỤC
-- ============================================================

-- ------------------------------------------------------------
-- 2.1. VAI_TRO - Vai trò phân quyền
-- Đạt chuẩn BCNF
-- ------------------------------------------------------------
CREATE TABLE dbo.vai_tro (
    ma_vai_tro              BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_vai_tro_hien_thi     NVARCHAR(30)                    NOT NULL,   -- RB: không trống, duy nhất
    ten_vai_tro             NVARCHAR(150)                   NOT NULL,
    mo_ta                   NVARCHAR(MAX)                   NULL,
    la_vai_tro_he_thong     BIT                             NOT NULL    DEFAULT 0,
    trang_thai              BIT                             NOT NULL    DEFAULT 1,
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_vai_tro               PRIMARY KEY (ma_vai_tro),
    CONSTRAINT UQ_vai_tro_hien_thi      UNIQUE      (ma_vai_tro_hien_thi),
    CONSTRAINT CK_vai_tro_ten           CHECK       (LEN(LTRIM(RTRIM(ten_vai_tro))) > 0)
);
GO


-- ------------------------------------------------------------
-- 2.2. CHI_NHANH - Chi nhánh / Phòng giao dịch
-- Đạt chuẩn BCNF; RB3
-- ------------------------------------------------------------
CREATE TABLE dbo.chi_nhanh (
    ma_chi_nhanh            BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_chi_nhanh_hien_thi   NVARCHAR(20)                    NOT NULL,
    ten_chi_nhanh           NVARCHAR(255)                   NOT NULL,
    dia_chi                 NVARCHAR(MAX)                   NULL,
    so_dien_thoai           NVARCHAR(20)                    NULL,
    truong_chi_nhanh        NVARCHAR(255)                   NULL,
    vung_dia_ly             NVARCHAR(100)                   NULL,
    -- Thay ENUM bằng CHECK constraint
    trang_thai              NVARCHAR(20)                    NOT NULL    DEFAULT N'HOAT_DONG',
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_chi_nhanh             PRIMARY KEY (ma_chi_nhanh),
    CONSTRAINT UQ_chi_nhanh_hien_thi    UNIQUE      (ma_chi_nhanh_hien_thi),
    -- RB3: số điện thoại đúng 10 chữ số
    CONSTRAINT CK_chi_nhanh_sdt         CHECK (
        so_dien_thoai IS NULL OR
        LEN(so_dien_thoai) = 10 AND so_dien_thoai NOT LIKE N'%[^0-9]%'
    ),
    CONSTRAINT CK_chi_nhanh_trangthai   CHECK (
        trang_thai IN (N'HOAT_DONG', N'DA_DONG', N'BAO_TRI')
    )
);
GO


-- ------------------------------------------------------------
-- 2.3. KHACH_HANG - Khách hàng cá nhân / doanh nghiệp
-- Đạt chuẩn BCNF; RB1, RB2, RB3, RB24
-- ------------------------------------------------------------
CREATE TABLE dbo.khach_hang (
    ma_khach_hang           BIGINT          IDENTITY(1,1)   NOT NULL,
    loai_khach_hang         NVARCHAR(20)                    NOT NULL,   -- CA_NHAN | DOANH_NGHIEP
    ho_ten                  NVARCHAR(255)                   NOT NULL,   -- RB: NOT NULL
    so_giay_to              NVARCHAR(50)                    NOT NULL,   -- CCCD / MST
    gioi_tinh               NVARCHAR(10)                    NULL,
    ngay_sinh               DATE                            NULL,
    so_dien_thoai           NVARCHAR(20)                    NULL,
    email                   NVARCHAR(255)                   NULL,
    dia_chi                 NVARCHAR(MAX)                   NULL,
    nghe_nghiep             NVARCHAR(255)                   NULL,
    muc_do_rui_ro           NVARCHAR(20)                    NOT NULL    DEFAULT N'Trung bình',
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_khach_hang            PRIMARY KEY (ma_khach_hang),
    CONSTRAINT UQ_khach_hang_giayto     UNIQUE      (so_giay_to),
    CONSTRAINT UQ_khach_hang_email      UNIQUE      (email),
    -- RB1: giới tính chỉ Nam hoặc Nữ
    CONSTRAINT CK_khach_hang_gioitinh   CHECK (
        gioi_tinh IS NULL OR gioi_tinh IN (N'Nam', N'Nữ')
    ),
    -- RB: loại khách hàng hợp lệ
    CONSTRAINT CK_khach_hang_loai       CHECK (
        loai_khach_hang IN (N'CA_NHAN', N'DOANH_NGHIEP')
    ),
    -- RB2: CCCD 9 hoặc 12 số (áp dụng khi chỉ chứa chữ số)
    CONSTRAINT CK_khach_hang_giayto     CHECK (
        so_giay_to NOT LIKE N'%[^0-9]%' AND LEN(so_giay_to) IN (9, 12)
        OR so_giay_to LIKE N'%[^0-9]%'   -- MST / hộ chiếu có ký tự chữ
    ),
    -- RB3: số điện thoại 10 số
    CONSTRAINT CK_khach_hang_sdt        CHECK (
        so_dien_thoai IS NULL OR
        (LEN(so_dien_thoai) = 10 AND so_dien_thoai NOT LIKE N'%[^0-9]%')
    ),
    -- RB24: email phải có '@' và '.'
    CONSTRAINT CK_khach_hang_email      CHECK (
        email IS NULL OR
        (email LIKE N'%@%.%')
    ),
    -- RB40: mức độ rủi ro hợp lệ
    CONSTRAINT CK_khach_hang_ruiro      CHECK (
        muc_do_rui_ro IN (N'Thấp', N'Trung bình', N'Cao')
    )
);
GO


-- ------------------------------------------------------------
-- 2.4. SAN_PHAM_VAY - Sản phẩm vay
-- Đạt chuẩn BCNF; RB4, RB5
-- ------------------------------------------------------------
CREATE TABLE dbo.san_pham_vay (
    ma_san_pham             BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_san_pham_vay         NVARCHAR(30)                    NOT NULL,   -- mã ngắn, duy nhất
    ten_san_pham            NVARCHAR(255)                   NOT NULL,
    mo_ta                   NVARCHAR(MAX)                   NULL,
    lai_suat                DECIMAL(6,4)                    NOT NULL,   -- RB4: 0 < x <= 0.30
    ky_han_toi_thieu        INT                             NOT NULL,   -- RB5: >= 1
    ky_han_toi_da           INT                             NOT NULL,   -- RB5: > ky_han_toi_thieu
    han_muc_toi_thieu       DECIMAL(18,2)                   NOT NULL,
    han_muc_toi_da          DECIMAL(18,2)                   NULL,       -- NULL = không giới hạn
    doi_tuong_kh            NVARCHAR(255)                   NULL,
    yeu_cau_tsdb            BIT                             NOT NULL    DEFAULT 0,
    trang_thai              NVARCHAR(20)                    NOT NULL    DEFAULT N'HOAT_DONG',
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_san_pham_vay          PRIMARY KEY (ma_san_pham),
    CONSTRAINT UQ_san_pham_ma           UNIQUE      (ma_san_pham_vay),
    -- RB4
    CONSTRAINT CK_spv_laisuat           CHECK (lai_suat > 0 AND lai_suat <= 0.30),
    -- RB5
    CONSTRAINT CK_spv_kyhan             CHECK (
        ky_han_toi_thieu >= 1 AND ky_han_toi_da > ky_han_toi_thieu
    ),
    CONSTRAINT CK_spv_hanmuc            CHECK (
        han_muc_toi_thieu > 0 AND
        (han_muc_toi_da IS NULL OR han_muc_toi_da >= han_muc_toi_thieu)
    ),
    CONSTRAINT CK_spv_trangthai         CHECK (
        trang_thai IN (N'HOAT_DONG', N'NGUNG_HOAT_DONG', N'NGUNG_CUNG_CAP')
    )
);
GO


-- ============================================================
-- PHẦN 3: BẢNG NHÂN VIÊN
-- ============================================================

-- ------------------------------------------------------------
-- 3.1. NGUOI_DUNG - Tài khoản nhân viên ngân hàng
-- Đạt chuẩn BCNF; RB22, RB23, RB37
-- ------------------------------------------------------------
CREATE TABLE dbo.nguoi_dung (
    ma_nguoi_dung           BIGINT          IDENTITY(1,1)   NOT NULL,
    ten_dang_nhap           NVARCHAR(100)                   NOT NULL,   -- RB22
    mat_khau_bam            NVARCHAR(255)                   NOT NULL,   -- RB23: bcrypt hash
    ho_ten                  NVARCHAR(255)                   NOT NULL,
    ma_vai_tro              BIGINT                          NOT NULL,
    ma_chi_nhanh            BIGINT                          NOT NULL,
    email                   NVARCHAR(255)                   NOT NULL,
    so_dien_thoai           NVARCHAR(20)                    NULL,
    lan_dang_nhap_cuoi      DATETIME2                       NULL,
    trang_thai              NVARCHAR(20)                    NOT NULL    DEFAULT N'HOAT_DONG',
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_nguoi_dung            PRIMARY KEY (ma_nguoi_dung),
    CONSTRAINT UQ_nguoi_dung_tendangnhap UNIQUE     (ten_dang_nhap),
    CONSTRAINT UQ_nguoi_dung_email      UNIQUE      (email),
    -- RB37: phải thuộc chi nhánh tồn tại
    CONSTRAINT FK_nguoidung_chinhanh    FOREIGN KEY (ma_chi_nhanh)
        REFERENCES dbo.chi_nhanh(ma_chi_nhanh),
    CONSTRAINT FK_nguoidung_vaitro      FOREIGN KEY (ma_vai_tro)
        REFERENCES dbo.vai_tro(ma_vai_tro),
    CONSTRAINT CK_nguoidung_trangthai   CHECK (
        trang_thai IN (N'HOAT_DONG', N'NGUNG_HOAT_DONG', N'TAM_KHOA')
    ),
    CONSTRAINT CK_nguoidung_sdt         CHECK (
        so_dien_thoai IS NULL OR
        (LEN(so_dien_thoai) = 10 AND so_dien_thoai NOT LIKE N'%[^0-9]%')
    )
);
GO


-- ============================================================
-- PHẦN 4: HỒ SƠ VAY VÀ CÁC THỰC THỂ LIÊN QUAN
-- ============================================================

-- ------------------------------------------------------------
-- 4.1. HO_SO_VAY - Hồ sơ đề nghị cấp tín dụng
-- Đạt chuẩn 3NF; RB6, RB7, RB28, RB29
-- ------------------------------------------------------------
CREATE TABLE dbo.ho_so_vay (
    ma_ho_so                BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_ho_so_hien_thi       NVARCHAR(30)                    NOT NULL,
    ma_khach_hang           BIGINT                          NOT NULL,   -- RB28
    ma_san_pham             BIGINT                          NOT NULL,   -- RB29
    ma_can_bo               BIGINT                          NOT NULL,
    so_tien_de_nghi         DECIMAL(18,2)                   NOT NULL,   -- RB6: > 0
    ky_han_thang            INT                             NOT NULL,   -- > 0
    muc_dich                NVARCHAR(MAX)                   NOT NULL,
    trang_thai              NVARCHAR(30)                    NOT NULL    DEFAULT N'MOI',
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_nop                DATETIME2                       NULL,
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ho_so_vay             PRIMARY KEY (ma_ho_so),
    CONSTRAINT UQ_ho_so_ma_hien_thi     UNIQUE      (ma_ho_so_hien_thi),
    -- RB28
    CONSTRAINT FK_hosovay_khachhang     FOREIGN KEY (ma_khach_hang)
        REFERENCES dbo.khach_hang(ma_khach_hang),
    -- RB29
    CONSTRAINT FK_hosovay_sanpham       FOREIGN KEY (ma_san_pham)
        REFERENCES dbo.san_pham_vay(ma_san_pham),
    CONSTRAINT FK_hosovay_canbo         FOREIGN KEY (ma_can_bo)
        REFERENCES dbo.nguoi_dung(ma_nguoi_dung),
    -- RB6
    CONSTRAINT CK_hosovay_sotien        CHECK (so_tien_de_nghi > 0),
    CONSTRAINT CK_hosovay_kyhan         CHECK (ky_han_thang > 0),
    -- RB7: trạng thái hợp lệ
    CONSTRAINT CK_hosovay_trangthai     CHECK (trang_thai IN (
        N'MOI', N'DA_NOP', N'DANG_XEM_XET', N'DU_TAI_LIEU',
        N'DANG_THAM_DINH', N'CHO_PHE_DUYET', N'DA_PHE_DUYET',
        N'TU_CHOI', N'DA_LAP_HOP_DONG', N'DA_GIAI_NGAN', N'DA_DONG'
    ))
);
GO

CREATE INDEX IDX_hosovay_khachhang  ON dbo.ho_so_vay(ma_khach_hang);
CREATE INDEX IDX_hosovay_trangthai  ON dbo.ho_so_vay(trang_thai);
CREATE INDEX IDX_hosovay_canbo      ON dbo.ho_so_vay(ma_can_bo);
GO


-- ------------------------------------------------------------
-- 4.2. TAI_LIEU_VAY - Chứng từ / giấy tờ đính kèm hồ sơ
-- Đạt chuẩn BCNF
-- ------------------------------------------------------------
CREATE TABLE dbo.tai_lieu_vay (
    ma_tai_lieu             BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_ho_so                BIGINT                          NOT NULL,
    loai_tai_lieu           NVARCHAR(50)                    NOT NULL,
    ten_tap_tin             NVARCHAR(255)                   NOT NULL,
    duong_dan_tap_tin       NVARCHAR(MAX)                   NOT NULL,
    kich_thuoc_byte         BIGINT                          NULL,
    dinh_dang               NVARCHAR(20)                    NULL,
    phien_ban               INT                             NOT NULL    DEFAULT 1,
    trang_thai_xac_minh     NVARCHAR(20)                    NOT NULL    DEFAULT N'CHO_KIEM_TRA',
    nguoi_tai_len           BIGINT                          NULL,
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_tai_lieu_vay          PRIMARY KEY (ma_tai_lieu),
    CONSTRAINT FK_tailieu_hosovay       FOREIGN KEY (ma_ho_so)
        REFERENCES dbo.ho_so_vay(ma_ho_so),
    CONSTRAINT FK_tailieu_nguoitailen   FOREIGN KEY (nguoi_tai_len)
        REFERENCES dbo.nguoi_dung(ma_nguoi_dung),
    CONSTRAINT CK_tailieu_phienban      CHECK (phien_ban >= 1),
    CONSTRAINT CK_tailieu_trangthai     CHECK (
        trang_thai_xac_minh IN (N'CHO_KIEM_TRA', N'DA_XAC_MINH', N'TU_CHOI')
    )
);
GO

CREATE INDEX IDX_tailieu_hosovay ON dbo.tai_lieu_vay(ma_ho_so);
GO


-- ------------------------------------------------------------
-- 4.3. TAI_SAN_DAM_BAO - Tài sản thế chấp
-- Đạt chuẩn BCNF; RB18, RB19, RB34
-- ------------------------------------------------------------
CREATE TABLE dbo.tai_san_dam_bao (
    ma_tai_san              BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_ho_so                BIGINT                          NOT NULL,   -- RB34
    loai_tai_san            NVARCHAR(50)                    NOT NULL,
    mo_ta                   NVARCHAR(MAX)                   NULL,
    dia_chi_tai_san         NVARCHAR(MAX)                   NULL,
    gia_tri_uoc_tinh        DECIMAL(18,2)                   NOT NULL,   -- RB19: > 0
    ngay_dinh_gia           DATE                            NULL,
    don_vi_dinh_gia         NVARCHAR(255)                   NULL,
    tinh_trang_phap_ly      NVARCHAR(100)                   NOT NULL,
    so_chung_nhan_sh        NVARCHAR(100)                   NULL,
    ty_le_vay_gia_tri_ts    DECIMAL(5,4)                    NULL,       -- RB18: 0..1
    so_hop_dong_bh          NVARCHAR(100)                   NULL,
    ngay_het_han_bh         DATE                            NULL,
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_tai_san_dam_bao       PRIMARY KEY (ma_tai_san),
    -- RB34
    CONSTRAINT FK_tsdb_hosovay          FOREIGN KEY (ma_ho_so)
        REFERENCES dbo.ho_so_vay(ma_ho_so),
    -- RB19
    CONSTRAINT CK_tsdb_giatri           CHECK (gia_tri_uoc_tinh > 0),
    -- RB18
    CONSTRAINT CK_tsdb_ltv              CHECK (
        ty_le_vay_gia_tri_ts IS NULL OR
        (ty_le_vay_gia_tri_ts >= 0 AND ty_le_vay_gia_tri_ts <= 1)
    )
);
GO

CREATE INDEX IDX_tsdb_hosovay ON dbo.tai_san_dam_bao(ma_ho_so);
GO


-- ------------------------------------------------------------
-- 4.4. THAM_DINH_TIN_DUNG - Kết quả thẩm định tín dụng
-- Đạt chuẩn BCNF; RB15, RB16, RB17, RB33
-- ------------------------------------------------------------
CREATE TABLE dbo.tham_dinh_tin_dung (
    ma_tham_dinh            BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_ho_so                BIGINT                          NOT NULL,   -- UNIQUE: 1-1 với hồ sơ
    ma_can_bo_tham_dinh     BIGINT                          NOT NULL,
    thu_nhap_xac_minh       DECIMAL(18,2)                   NULL,
    chi_phi_hang_thang      DECIMAL(18,2)                   NULL,
    ty_le_no_thu_nhap       DECIMAL(6,4)                    NULL,       -- DTI: 0..1
    diem_thu_nhap           DECIMAL(5,2)                    NULL,       -- RB15: 0..100
    diem_tai_san_bao_dam    DECIMAL(5,2)                    NULL,       -- RB16: 0..100
    diem_cic                DECIMAL(5,2)                    NULL,
    ngay_tra_cuu_cic        DATE                            NULL,
    diem_rui_ro             DECIMAL(5,2)                    NOT NULL,   -- RB17: 0..100
    khuyen_nghi             NVARCHAR(30)                    NULL,
    ghi_chu                 NVARCHAR(MAX)                   NULL,
    ngay_tham_dinh          DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_tham_dinh_tin_dung    PRIMARY KEY (ma_tham_dinh),
    -- RB33: mỗi hồ sơ chỉ có 1 bản thẩm định
    CONSTRAINT UQ_thamdinh_hosovay      UNIQUE      (ma_ho_so),
    CONSTRAINT FK_thamdinh_hosovay      FOREIGN KEY (ma_ho_so)
        REFERENCES dbo.ho_so_vay(ma_ho_so),
    CONSTRAINT FK_thamdinh_canbo        FOREIGN KEY (ma_can_bo_tham_dinh)
        REFERENCES dbo.nguoi_dung(ma_nguoi_dung),
    -- RB15
    CONSTRAINT CK_thamdinh_diemthunhap  CHECK (
        diem_thu_nhap IS NULL OR (diem_thu_nhap >= 0 AND diem_thu_nhap <= 100)
    ),
    -- RB16
    CONSTRAINT CK_thamdinh_diemtsdb     CHECK (
        diem_tai_san_bao_dam IS NULL OR
        (diem_tai_san_bao_dam >= 0 AND diem_tai_san_bao_dam <= 100)
    ),
    -- RB17
    CONSTRAINT CK_thamdinh_diemruiro    CHECK (
        diem_rui_ro >= 0 AND diem_rui_ro <= 100
    ),
    CONSTRAINT CK_thamdinh_thunhap      CHECK (
        thu_nhap_xac_minh IS NULL OR thu_nhap_xac_minh >= 0
    ),
    CONSTRAINT CK_thamdinh_chiphi       CHECK (
        chi_phi_hang_thang IS NULL OR chi_phi_hang_thang >= 0
    ),
    CONSTRAINT CK_thamdinh_dti          CHECK (
        ty_le_no_thu_nhap IS NULL OR
        (ty_le_no_thu_nhap >= 0 AND ty_le_no_thu_nhap <= 1)
    ),
    CONSTRAINT CK_thamdinh_khuyen_nghi  CHECK (
        khuyen_nghi IS NULL OR khuyen_nghi IN (
            N'DE_NGHI_DUYET', N'DE_NGHI_TU_CHOI',
            N'CAN_BO_SUNG', N'CHUYEN_HOI_DONG_RUI_RO'
        )
    )
);
GO


-- ------------------------------------------------------------
-- 4.5. PHE_DUYET - Quyết định phê duyệt khoản vay
-- Đạt chuẩn BCNF; RB20, RB21, RB35, RB36
-- ------------------------------------------------------------
CREATE TABLE dbo.phe_duyet (
    ma_phe_duyet            BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_ho_so                BIGINT                          NOT NULL,   -- RB35
    ma_nguoi_phe_duyet      BIGINT                          NOT NULL,   -- RB36
    cap_phe_duyet           INT                             NOT NULL,
    quyet_dinh              NVARCHAR(20)                    NOT NULL,   -- RB20
    so_tien_duoc_duyet      DECIMAL(18,2)                   NULL,       -- RB21
    lai_suat_duoc_duyet     DECIMAL(6,4)                    NULL,
    dieu_kien_phe_duyet     NVARCHAR(MAX)                   NULL,
    ly_do_tu_choi           NVARCHAR(MAX)                   NULL,
    ngay_phe_duyet          DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_phe_duyet             PRIMARY KEY (ma_phe_duyet),
    -- RB35
    CONSTRAINT FK_pheduyet_hosovay      FOREIGN KEY (ma_ho_so)
        REFERENCES dbo.ho_so_vay(ma_ho_so),
    -- RB36
    CONSTRAINT FK_pheduyet_nguoidung    FOREIGN KEY (ma_nguoi_phe_duyet)
        REFERENCES dbo.nguoi_dung(ma_nguoi_dung),
    CONSTRAINT CK_pheduyet_cap          CHECK (cap_phe_duyet > 0),
    -- RB20
    CONSTRAINT CK_pheduyet_quyetdinh   CHECK (
        quyet_dinh IN (N'DA_PHE_DUYET', N'TU_CHOI', N'YEU_CAU_BO_SUNG')
    ),
    -- RB21: số tiền phê duyệt > 0 khi quyết định là DA_PHE_DUYET
    CONSTRAINT CK_pheduyet_sotien       CHECK (
        quyet_dinh <> N'DA_PHE_DUYET' OR
        (so_tien_duoc_duyet IS NOT NULL AND so_tien_duoc_duyet > 0)
    ),
    CONSTRAINT CK_pheduyet_laisuat      CHECK (
        lai_suat_duoc_duyet IS NULL OR
        (lai_suat_duoc_duyet > 0 AND lai_suat_duoc_duyet <= 0.30)
    )
);
GO

CREATE INDEX IDX_pheduyet_hosovay ON dbo.phe_duyet(ma_ho_so);
GO


-- ============================================================
-- PHẦN 5: HỢP ĐỒNG VÀ THEO DÕI SAU VAY
-- ============================================================

-- ------------------------------------------------------------
-- 5.1. HOP_DONG_VAY - Hợp đồng tín dụng
-- Đạt chuẩn 3NF; RB8, RB10, RB30
-- ------------------------------------------------------------
CREATE TABLE dbo.hop_dong_vay (
    ma_hop_dong             BIGINT          IDENTITY(1,1)   NOT NULL,
    so_hop_dong             NVARCHAR(50)                    NOT NULL,   -- RB8
    ma_ho_so                BIGINT                          NOT NULL,   -- RB30: 1-1
    tien_goc                DECIMAL(18,2)                   NOT NULL,   -- RB10: > 0
    lai_suat                DECIMAL(6,4)                    NOT NULL,
    ngay_bat_dau            DATE                            NOT NULL,
    ngay_dao_han            DATE                            NOT NULL,
    ngay_giai_ngan          DATETIME2                       NULL,
    tong_phai_tra           DECIMAL(18,2)                   NULL,
    trang_thai              NVARCHAR(20)                    NOT NULL    DEFAULT N'HOAT_DONG',
    ghi_chu                 NVARCHAR(MAX)                   NULL,
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_hop_dong_vay          PRIMARY KEY (ma_hop_dong),
    -- RB8: số hợp đồng không trống, duy nhất
    CONSTRAINT UQ_hop_dong_so           UNIQUE      (so_hop_dong),
    -- RB30: 1-1 với hồ sơ
    CONSTRAINT UQ_hop_dong_hosovay      UNIQUE      (ma_ho_so),
    CONSTRAINT FK_hopdong_hosovay       FOREIGN KEY (ma_ho_so)
        REFERENCES dbo.ho_so_vay(ma_ho_so),
    -- RB10
    CONSTRAINT CK_hopdong_tiengoc       CHECK (tien_goc > 0),
    CONSTRAINT CK_hopdong_laisuat       CHECK (lai_suat > 0 AND lai_suat <= 0.30),
    CONSTRAINT CK_hopdong_ngaodaohan    CHECK (ngay_dao_han > ngay_bat_dau),
    CONSTRAINT CK_hopdong_trangthai     CHECK (trang_thai IN (
        N'HOAT_DONG', N'QUA_HAN', N'QUA_HAN_30',
        N'QUA_HAN_60', N'DA_DONG', N'DA_HUY'
    ))
);
GO

CREATE INDEX IDX_hopdong_trangthai ON dbo.hop_dong_vay(trang_thai);
GO


-- ------------------------------------------------------------
-- 5.2. LICH_TRA_NO - Lịch trả nợ từng kỳ
-- Đạt chuẩn BCNF; RB12, RB13, RB31
-- ------------------------------------------------------------
CREATE TABLE dbo.lich_tra_no (
    ma_lich_tra_no          BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_hop_dong             BIGINT                          NOT NULL,   -- RB31
    ky_thanh_toan           INT                             NOT NULL,
    ngay_den_han            DATE                            NOT NULL,
    goc_phai_tra            DECIMAL(18,2)                   NOT NULL,   -- RB12: >= 0
    lai_phai_tra            DECIMAL(18,2)                   NOT NULL,   -- RB13: >= 0
    phi_phat                DECIMAL(18,2)                   NOT NULL    DEFAULT 0,
    trang_thai              NVARCHAR(30)                    NOT NULL    DEFAULT N'CHUA_THANH_TOAN',
    han_thanh_toan          DATE                            NULL,
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_lich_tra_no           PRIMARY KEY (ma_lich_tra_no),
    -- RB31
    CONSTRAINT FK_lichtra_hopdong       FOREIGN KEY (ma_hop_dong)
        REFERENCES dbo.hop_dong_vay(ma_hop_dong),
    -- mỗi kỳ trong hợp đồng là duy nhất
    CONSTRAINT UQ_lichtra_ky            UNIQUE (ma_hop_dong, ky_thanh_toan),
    CONSTRAINT CK_lichtra_kytt          CHECK (ky_thanh_toan > 0),
    -- RB12
    CONSTRAINT CK_lichtra_goc           CHECK (goc_phai_tra >= 0),
    -- RB13
    CONSTRAINT CK_lichtra_lai           CHECK (lai_phai_tra >= 0),
    CONSTRAINT CK_lichtra_phi           CHECK (phi_phat >= 0),
    CONSTRAINT CK_lichtra_trangthai     CHECK (trang_thai IN (
        N'CHUA_THANH_TOAN', N'DA_THANH_TOAN',
        N'TRE_HAN', N'THANH_TOAN_MOT_PHAN'
    ))
);
GO

CREATE INDEX IDX_lichtra_hopdong    ON dbo.lich_tra_no(ma_hop_dong);
CREATE INDEX IDX_lichtra_ngaydenhan ON dbo.lich_tra_no(ngay_den_han);
CREATE INDEX IDX_lichtra_trangthai  ON dbo.lich_tra_no(trang_thai);
GO


-- ------------------------------------------------------------
-- 5.3. THANH_TOAN - Giao dịch thu nợ
-- Đạt chuẩn BCNF; RB14, RB32
-- ------------------------------------------------------------
CREATE TABLE dbo.thanh_toan (
    ma_thanh_toan           BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_lich_tra_no          BIGINT                          NOT NULL,   -- RB32
    ma_hop_dong             BIGINT                          NOT NULL,
    ma_ho_so                BIGINT                          NOT NULL,
    so_tien_da_tra          DECIMAL(18,2)                   NOT NULL,   -- RB14: > 0
    so_tien_ap_goc          DECIMAL(18,2)                   NOT NULL    DEFAULT 0,
    so_tien_ap_lai          DECIMAL(18,2)                   NOT NULL    DEFAULT 0,
    so_tien_ap_phi          DECIMAL(18,2)                   NOT NULL    DEFAULT 0,
    ngay_thanh_toan         DATETIME2                       NOT NULL,
    phuong_thuc_thanh_toan  NVARCHAR(30)                    NULL,
    ma_giao_dich            NVARCHAR(100)                   NULL,
    kenh_thanh_toan         NVARCHAR(100)                   NULL,
    nguoi_thu               BIGINT                          NULL,
    ghi_chu                 NVARCHAR(MAX)                   NULL,
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),
    ngay_cap_nhat           DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_thanh_toan            PRIMARY KEY (ma_thanh_toan),
    CONSTRAINT UQ_thanhtoan_magiaodich  UNIQUE      (ma_giao_dich),
    -- RB32
    CONSTRAINT FK_thanhtoan_lichtra     FOREIGN KEY (ma_lich_tra_no)
        REFERENCES dbo.lich_tra_no(ma_lich_tra_no),
    CONSTRAINT FK_thanhtoan_hopdong     FOREIGN KEY (ma_hop_dong)
        REFERENCES dbo.hop_dong_vay(ma_hop_dong),
    CONSTRAINT FK_thanhtoan_hosovay     FOREIGN KEY (ma_ho_so)
        REFERENCES dbo.ho_so_vay(ma_ho_so),
    CONSTRAINT FK_thanhtoan_nguoithu    FOREIGN KEY (nguoi_thu)
        REFERENCES dbo.nguoi_dung(ma_nguoi_dung),
    -- RB14
    CONSTRAINT CK_thanhtoan_sotien      CHECK (so_tien_da_tra > 0),
    CONSTRAINT CK_thanhtoan_goc_nn      CHECK (so_tien_ap_goc >= 0),
    CONSTRAINT CK_thanhtoan_lai_nn      CHECK (so_tien_ap_lai >= 0),
    CONSTRAINT CK_thanhtoan_phi_nn      CHECK (so_tien_ap_phi >= 0),
    -- gốc + lãi + phí = tổng đã trả (sai số < 0.01)
    CONSTRAINT CK_thanhtoan_phanbo      CHECK (
        ABS((so_tien_ap_goc + so_tien_ap_lai + so_tien_ap_phi) - so_tien_da_tra) < 0.01
    ),
    CONSTRAINT CK_thanhtoan_phuongthuc  CHECK (
        phuong_thuc_thanh_toan IS NULL OR
        phuong_thuc_thanh_toan IN (
            N'TIEN_MAT', N'CHUYEN_KHOAN', N'THE',
            N'TU_DONG_TRICH_NO', N'CHUYEN_TIEN_LIEN_NGAN_HANG'
        )
    )
);
GO

CREATE INDEX IDX_thanhtoan_lichtra  ON dbo.thanh_toan(ma_lich_tra_no);
CREATE INDEX IDX_thanhtoan_hopdong  ON dbo.thanh_toan(ma_hop_dong);
CREATE INDEX IDX_thanhtoan_ngaytt   ON dbo.thanh_toan(ngay_thanh_toan);
GO


-- ============================================================
-- PHẦN 6: THÔNG BÁO VÀ NHẬT KÝ
-- ============================================================

-- ------------------------------------------------------------
-- 6.1. THONG_BAO
-- Đạt chuẩn BCNF
-- ------------------------------------------------------------
CREATE TABLE dbo.thong_bao (
    ma_thong_bao            BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_nguoi_nhan           BIGINT                          NOT NULL,
    loai_nguoi_nhan         NVARCHAR(20)                    NOT NULL    DEFAULT N'NGUOI_DUNG',
    tieu_de                 NVARCHAR(255)                   NOT NULL,
    noi_dung                NVARCHAR(MAX)                   NOT NULL,
    loai                    NVARCHAR(10)                    NOT NULL    DEFAULT N'INAPP',
    da_doc                  BIT                             NOT NULL    DEFAULT 0,
    ma_ho_so                BIGINT                          NULL,
    ngay_gui                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_thong_bao             PRIMARY KEY (ma_thong_bao),
    CONSTRAINT FK_thongbao_hosovay      FOREIGN KEY (ma_ho_so)
        REFERENCES dbo.ho_so_vay(ma_ho_so),
    CONSTRAINT CK_thongbao_loainguoinhan CHECK (
        loai_nguoi_nhan IN (N'KHACH_HANG', N'NGUOI_DUNG', N'ALL')
    ),
    CONSTRAINT CK_thongbao_loai         CHECK (
        loai IN (N'SMS', N'EMAIL', N'PUSH', N'PAGE', N'INAPP')
    )
);
GO

CREATE INDEX IDX_thongbao_nguoinhan ON dbo.thong_bao(ma_nguoi_nhan, loai_nguoi_nhan);
GO


-- ------------------------------------------------------------
-- 6.2. NHAT_KY_HE_THONG - Audit log (APPEND-ONLY)
-- Đạt chuẩn BCNF
-- ------------------------------------------------------------
CREATE TABLE dbo.nhat_ky_he_thong (
    ma_nhat_ky              BIGINT          IDENTITY(1,1)   NOT NULL,
    ma_nguoi_dung           BIGINT                          NULL,
    hanh_dong               NVARCHAR(50)                    NOT NULL,
    ten_thuc_the            NVARCHAR(80)                    NOT NULL,
    ma_thuc_the             NVARCHAR(80)                    NOT NULL,
    gia_tri_cu              NVARCHAR(MAX)                   NULL,   -- JSON string
    gia_tri_moi             NVARCHAR(MAX)                   NULL,   -- JSON string
    truong_thay_doi         NVARCHAR(MAX)                   NULL,
    ip_nguon                NVARCHAR(45)                    NULL,
    trinh_duyet             NVARCHAR(MAX)                   NULL,
    ngay_tao                DATETIME2                       NOT NULL    DEFAULT SYSDATETIME(),

    CONSTRAINT PK_nhat_ky_he_thong      PRIMARY KEY (ma_nhat_ky),
    CONSTRAINT FK_nhatky_nguoidung      FOREIGN KEY (ma_nguoi_dung)
        REFERENCES dbo.nguoi_dung(ma_nguoi_dung),
    CONSTRAINT CK_nhatky_hanhdong       CHECK (hanh_dong IN (
        N'CREATE', N'UPDATE', N'DELETE', N'APPROVE',
        N'REJECT', N'LOGIN', N'LOGOUT', N'EXPORT', N'DISBURSE'
    ))
);
GO

CREATE INDEX IDX_nhatky_nguoidung   ON dbo.nhat_ky_he_thong(ma_nguoi_dung);
CREATE INDEX IDX_nhatky_thucthe     ON dbo.nhat_ky_he_thong(ten_thuc_the, ma_thuc_the);
CREATE INDEX IDX_nhatky_ngaytao     ON dbo.nhat_ky_he_thong(ngay_tao DESC);
GO


-- ============================================================
-- PHẦN 7: TRIGGER NGHIỆP VỤ
-- ============================================================

-- ------------------------------------------------------------
-- T1: RB38/RB39 – Số tiền và kỳ hạn không vượt giới hạn sản phẩm
-- ------------------------------------------------------------
GO
CREATE OR ALTER TRIGGER dbo.TRG_hosovay_check_sanpham
ON dbo.ho_so_vay
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.san_pham_vay sp ON sp.ma_san_pham = i.ma_san_pham
        WHERE
            -- RB38: vượt hạn mức tối đa
            (sp.han_muc_toi_da IS NOT NULL AND i.so_tien_de_nghi > sp.han_muc_toi_da)
            OR i.so_tien_de_nghi < sp.han_muc_toi_thieu
            -- RB39: vượt kỳ hạn tối đa
            OR i.ky_han_thang > sp.ky_han_toi_da
            OR i.ky_han_thang < sp.ky_han_toi_thieu
    )
    BEGIN
        RAISERROR (
            N'RB38/RB39: Số tiền hoặc kỳ hạn không hợp lệ theo sản phẩm vay đã chọn.',
            16, 1
        );
        ROLLBACK TRANSACTION;
    END
END;
GO


-- ------------------------------------------------------------
-- T2: RB42 – Kiểm soát luồng trạng thái hồ sơ vay
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.TRG_hosovay_trangthai
ON dbo.ho_so_vay
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Chỉ kiểm tra khi cột trang_thai thay đổi
    IF NOT UPDATE(trang_thai) RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.ma_ho_so = i.ma_ho_so
        WHERE d.trang_thai <> i.trang_thai
          AND NOT (
            -- Các chuyển trạng thái hợp lệ (RB42)
               (d.trang_thai = N'MOI'             AND i.trang_thai = N'DA_NOP')
            OR (d.trang_thai = N'DA_NOP'           AND i.trang_thai IN (N'DANG_XEM_XET', N'MOI'))
            OR (d.trang_thai = N'DANG_XEM_XET'    AND i.trang_thai IN (N'DU_TAI_LIEU', N'DA_NOP'))
            OR (d.trang_thai = N'DU_TAI_LIEU'     AND i.trang_thai = N'DANG_THAM_DINH')
            OR (d.trang_thai = N'DANG_THAM_DINH'  AND i.trang_thai IN (N'CHO_PHE_DUYET', N'DA_NOP'))
            OR (d.trang_thai = N'CHO_PHE_DUYET'   AND i.trang_thai IN (N'DA_PHE_DUYET', N'TU_CHOI'))
            OR (d.trang_thai = N'DA_PHE_DUYET'    AND i.trang_thai = N'DA_LAP_HOP_DONG')
            OR (d.trang_thai = N'DA_LAP_HOP_DONG' AND i.trang_thai = N'DA_GIAI_NGAN')
            OR (d.trang_thai = N'DA_GIAI_NGAN'    AND i.trang_thai = N'DA_DONG')
          )
    )
    BEGIN
        RAISERROR (
            N'RB42: Chuyển trạng thái hồ sơ vay không hợp lệ.',
            16, 1
        );
        ROLLBACK TRANSACTION;
    END
END;
GO


-- ------------------------------------------------------------
-- T3: RB44 – Hồ sơ phải có thẩm định trước khi phê duyệt
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.TRG_pheduyet_check_thamdinh
ON dbo.phe_duyet
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.tham_dinh_tin_dung t
            WHERE t.ma_ho_so = i.ma_ho_so
        )
    )
    BEGIN
        RAISERROR (
            N'RB44: Hồ sơ chưa có kết quả thẩm định tín dụng, không thể phê duyệt.',
            16, 1
        );
        ROLLBACK TRANSACTION;
    END
END;
GO


-- ------------------------------------------------------------
-- T4: RB40 – Cập nhật mức độ rủi ro khách hàng tự động
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.TRG_thamdinh_cap_nhat_ruiro
ON dbo.tham_dinh_tin_dung
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE kh
    SET kh.muc_do_rui_ro = CASE
        WHEN i.diem_rui_ro <= 30 THEN N'Thấp'
        WHEN i.diem_rui_ro <= 70 THEN N'Trung bình'
        ELSE N'Cao'
    END,
        kh.ngay_cap_nhat = SYSDATETIME()
    FROM dbo.khach_hang kh
    JOIN dbo.ho_so_vay  hs ON hs.ma_khach_hang = kh.ma_khach_hang
    JOIN inserted i         ON i.ma_ho_so       = hs.ma_ho_so;
END;
GO


-- ============================================================
-- PHẦN 8: DỮ LIỆU MẪU KHỞI TẠO
-- ============================================================

INSERT INTO dbo.vai_tro (ma_vai_tro_hien_thi, ten_vai_tro, mo_ta, la_vai_tro_he_thong)
VALUES
    (N'ADMIN',          N'Quản trị hệ thống',       N'Toàn quyền cấu hình và quản lý hệ thống',  1),
    (N'CREDIT_OFFICER', N'Cán bộ tín dụng',         N'Tiếp nhận và tạo hồ sơ vay',               1),
    (N'APPRAISER',      N'Thẩm định viên',           N'Thực hiện thẩm định tín dụng và TSBĐ',     1),
    (N'APPROVER_L1',    N'Phê duyệt cấp 1',          N'Phê duyệt khoản vay trong hạn mức cấp 1',  1),
    (N'APPROVER_L2',    N'Phê duyệt cấp 2',          N'Phê duyệt khoản vay trong hạn mức cấp 2',  1),
    (N'ACCOUNTANT',     N'Kế toán / Ngân quỹ',      N'Thực hiện giải ngân và ghi nhận thanh toán',1),
    (N'REPORT_VIEWER',  N'Xem báo cáo',              N'Chỉ xem báo cáo, không nhập liệu',         1);
GO

INSERT INTO dbo.chi_nhanh (ma_chi_nhanh_hien_thi, ten_chi_nhanh, dia_chi, so_dien_thoai, truong_chi_nhanh)
VALUES
    (N'HO',    N'Hội sở chính',                  N'123 Nguyễn Huệ, Q.1, TP.HCM',          N'0281234567', N'Nguyễn Văn A'),
    (N'HCM01', N'Chi nhánh HCM - Q.3',           N'45 Võ Văn Tần, Q.3, TP.HCM',           N'0287654321', N'Trần Thị B'),
    (N'HN01',  N'Chi nhánh Hà Nội - Hoàn Kiếm', N'10 Tràng Tiền, Hoàn Kiếm, HN',         N'0249876543', N'Lê Văn C');
GO

INSERT INTO dbo.san_pham_vay
    (ma_san_pham_vay, ten_san_pham, lai_suat, ky_han_toi_thieu, ky_han_toi_da,
     han_muc_toi_thieu, han_muc_toi_da, doi_tuong_kh, yeu_cau_tsdb)
VALUES
    (N'VTTD',  N'Vay tiêu dùng cá nhân',              0.12,  6,  60,  5000000,    500000000,  N'CA_NHAN',      0),
    (N'VKDD',  N'Vay kinh doanh doanh nghiệp',        0.10, 12, 120, 50000000, 5000000000,    N'DOANH_NGHIEP', 1),
    (N'VMNH',  N'Vay mua nhà',                         0.09, 12, 300, 100000000,  NULL,        N'CA_NHAN',      1),
    (N'VMXO',  N'Vay mua ô tô',                        0.10, 12,  84, 50000000, 2000000000,   N'CA_NHAN',      1),
    (N'VSTFD', N'Sản phẩm tài trợ siêu tốc KHDN nhỏ', 0.11,  3,  24, 10000000,  500000000,   N'DOANH_NGHIEP', 0);
GO
