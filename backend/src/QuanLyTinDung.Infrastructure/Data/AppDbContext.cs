using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Domain.Enums;

namespace QuanLyTinDung.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<SanPhamVay> SanPhamVays => Set<SanPhamVay>();
    public DbSet<ChiNhanh> ChiNhanhs => Set<ChiNhanh>();
    public DbSet<VaiTro> VaiTros => Set<VaiTro>();
    public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
    public DbSet<HoSoVay> HoSoVays => Set<HoSoVay>();
    public DbSet<ThamDinhTinDung> ThamDinhTinDungs => Set<ThamDinhTinDung>();
    public DbSet<PheDuyet> PheDuYets => Set<PheDuyet>();
    public DbSet<HopDongVay> HopDongVays => Set<HopDongVay>();
    public DbSet<LichTraNo> LichTraNos => Set<LichTraNo>();
    public DbSet<ThanhToan> ThanhToans => Set<ThanhToan>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<NhatKyHeThong> NhatKyHeThongs => Set<NhatKyHeThong>();
    public DbSet<TaiSanDamBao> TaiSanDamBaos => Set<TaiSanDamBao>();
    public DbSet<TaiLieuVay> TaiLieuVays => Set<TaiLieuVay>();

    /// <summary>
    /// Converter chấp nhận CẢ format cũ (có dấu + khoảng trắng) và format mới (enum string không dấu).
    /// Backward-compatible với dữ liệu đã tồn tại trong DB.
    /// </summary>
    private static string MucDoRuiRoToDb(MucDoRuiRo v)
    {
        if (v == MucDoRuiRo.Thap) return "Thấp";
        if (v == MucDoRuiRo.TrungBinh) return "Trung bình";
        if (v == MucDoRuiRo.Cao) return "Cao";
        return v.ToString();
    }

    private static MucDoRuiRo MucDoRuiRoFromDb(string v)
    {
        if (v == null) return MucDoRuiRo.TrungBinh;
        // Format MỚI (enum string không dấu)
        if (v == "Thap") return MucDoRuiRo.Thap;
        if (v == "TrungBinh") return MucDoRuiRo.TrungBinh;
        if (v == "Cao") return MucDoRuiRo.Cao;
        // Format CŨ (có dấu + khoảng trắng) - backward compat
        if (v == "Thấp") return MucDoRuiRo.Thap;
        if (v == "Trung bình" || v == "TrungBinh ") return MucDoRuiRo.TrungBinh;
        if (v == "Cao") return MucDoRuiRo.Cao;
        // Fallback an toàn
        return MucDoRuiRo.TrungBinh;
    }

    private static readonly ValueConverter<MucDoRuiRo, string> MucDoRuiRoConverter = new(
        v => MucDoRuiRoToDb(v),
        v => MucDoRuiRoFromDb(v));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // KhachHang
        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.ToTable("khach_hang");
            entity.HasKey(e => e.MaKhachHang);
            entity.Property(e => e.MaKhachHang).HasColumnName("ma_khach_hang");
            entity.Property(e => e.LoaiKhachHang).HasColumnName("loai_khach_hang").HasConversion<string>();
            entity.Property(e => e.HoTen).HasColumnName("ho_ten").HasMaxLength(255).IsRequired();
            entity.Property(e => e.SoGiayTo).HasColumnName("so_giay_to").HasMaxLength(50).IsRequired();
            entity.Property(e => e.GioiTinh).HasColumnName("gioi_tinh").HasMaxLength(10);
            entity.Property(e => e.NgaySinh).HasColumnName("ngay_sinh");
            entity.Property(e => e.SoDienThoai).HasColumnName("so_dien_thoai").HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.DiaChi).HasColumnName("dia_chi");
            entity.Property(e => e.NgheNghiep).HasColumnName("nghe_nghiep").HasMaxLength(255);
            entity.Property(e => e.MucDoRuiRo).HasColumnName("muc_do_rui_ro").HasMaxLength(20).HasConversion(MucDoRuiRoConverter);
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasIndex(e => e.SoGiayTo).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // SanPhamVay
        modelBuilder.Entity<SanPhamVay>(entity =>
        {
            entity.ToTable("san_pham_vay");
            entity.HasKey(e => e.MaSanPham);
            entity.Property(e => e.MaSanPham).HasColumnName("ma_san_pham");
            entity.Property(e => e.MaSanPhamVay).HasColumnName("ma_san_pham_vay").HasMaxLength(30).IsRequired();
            entity.Property(e => e.TenSanPham).HasColumnName("ten_san_pham").HasMaxLength(255).IsRequired();
            entity.Property(e => e.MoTa).HasColumnName("mo_ta");
            entity.Property(e => e.LaiSuat).HasColumnName("lai_suat").HasPrecision(6, 4);
            entity.Property(e => e.KyHanToiThieu).HasColumnName("ky_han_toi_thieu");
            entity.Property(e => e.KyHanToiDa).HasColumnName("ky_han_toi_da");
            entity.Property(e => e.HanMucToiThieu).HasColumnName("han_muc_toi_thieu").HasPrecision(18, 2);
            entity.Property(e => e.HanMucToiDa).HasColumnName("han_muc_toi_da").HasPrecision(18, 2);
            entity.Property(e => e.DoiTuongKh).HasColumnName("doi_tuong_kh").HasMaxLength(255);
            entity.Property(e => e.YeuCauTsdb).HasColumnName("yeu_cau_tsdb");
            entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(20);
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasIndex(e => e.MaSanPhamVay).IsUnique();
        });

        // ChiNhanh
        modelBuilder.Entity<ChiNhanh>(entity =>
        {
            entity.ToTable("chi_nhanh");
            entity.HasKey(e => e.MaChiNhanh);
            entity.Property(e => e.MaChiNhanh).HasColumnName("ma_chi_nhanh");
            entity.Property(e => e.MaChiNhanhHienThi).HasColumnName("ma_chi_nhanh_hien_thi").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TenChiNhanh).HasColumnName("ten_chi_nhanh").HasMaxLength(255).IsRequired();
            entity.Property(e => e.DiaChi).HasColumnName("dia_chi");
            entity.Property(e => e.SoDienThoai).HasColumnName("so_dien_thoai").HasMaxLength(20);
            entity.Property(e => e.TruongChiNhanh).HasColumnName("truong_chi_nhanh").HasMaxLength(255);
            entity.Property(e => e.VungDiaLy).HasColumnName("vung_dia_ly").HasMaxLength(100);
            entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(20);
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasIndex(e => e.MaChiNhanhHienThi).IsUnique();
        });

        // VaiTro
        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.ToTable("vai_tro");
            entity.HasKey(e => e.MaVaiTro);
            entity.Property(e => e.MaVaiTro).HasColumnName("ma_vai_tro");
            entity.Property(e => e.MaVaiTroHienThi).HasColumnName("ma_vai_tro_hien_thi").HasMaxLength(30).IsRequired();
            entity.Property(e => e.TenVaiTro).HasColumnName("ten_vai_tro").HasMaxLength(150).IsRequired();
            entity.Property(e => e.MoTa).HasColumnName("mo_ta");
            entity.Property(e => e.LaVaiTroHeThong).HasColumnName("la_vai_tro_he_thong");
            entity.Property(e => e.TrangThai).HasColumnName("trang_thai");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasIndex(e => e.MaVaiTroHienThi).IsUnique();
        });

        // NguoiDung
        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.ToTable("nguoi_dung");
            entity.HasKey(e => e.MaNguoiDung);
            entity.Property(e => e.MaNguoiDung).HasColumnName("ma_nguoi_dung");
            entity.Property(e => e.TenDangNhap).HasColumnName("ten_dang_nhap").HasMaxLength(100).IsRequired();
            entity.Property(e => e.MatKhauBam).HasColumnName("mat_khau_bam").HasMaxLength(255).IsRequired();
            entity.Property(e => e.HoTen).HasColumnName("ho_ten").HasMaxLength(255).IsRequired();
            entity.Property(e => e.MaVaiTro).HasColumnName("ma_vai_tro");
            entity.Property(e => e.MaChiNhanh).HasColumnName("ma_chi_nhanh");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.SoDienThoai).HasColumnName("so_dien_thoai").HasMaxLength(20);
            entity.Property(e => e.LanDangNhapCuoi).HasColumnName("lan_dang_nhap_cuoi");
            entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(20);
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasIndex(e => e.TenDangNhap).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.VaiTro).WithMany(v => v.NguoiDungs).HasForeignKey(e => e.MaVaiTro);
            entity.HasOne(e => e.ChiNhanh).WithMany(c => c.NguoiDungs).HasForeignKey(e => e.MaChiNhanh);
        });

        // HoSoVay
        modelBuilder.Entity<HoSoVay>(entity =>
        {
            entity.ToTable("ho_so_vay");
            entity.HasKey(e => e.MaHoSo);
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so");
            entity.Property(e => e.MaHoSoHienThi).HasColumnName("ma_ho_so_hien_thi").HasMaxLength(30).IsRequired();
            entity.Property(e => e.MaKhachHang).HasColumnName("ma_khach_hang");
            entity.Property(e => e.MaSanPham).HasColumnName("ma_san_pham");
            entity.Property(e => e.MaCanBo).HasColumnName("ma_can_bo");
            entity.Property(e => e.SoTienDeNghi).HasColumnName("so_tien_de_nghi").HasPrecision(18, 2);
            entity.Property(e => e.KyHanThang).HasColumnName("ky_han_thang");
            entity.Property(e => e.MucDich).HasColumnName("muc_dich").IsRequired();
            entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(30);
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayNop).HasColumnName("ngay_nop");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasIndex(e => e.MaHoSoHienThi).IsUnique();

            entity.HasOne(e => e.KhachHang).WithMany(k => k.HoSoVays).HasForeignKey(e => e.MaKhachHang);
            entity.HasOne(e => e.SanPhamVay).WithMany(s => s.HoSoVays).HasForeignKey(e => e.MaSanPham);
            entity.HasOne(e => e.CanBo).WithMany().HasForeignKey(e => e.MaCanBo);
        });

        // ThamDinhTinDung
        modelBuilder.Entity<ThamDinhTinDung>(entity =>
        {
            entity.ToTable("tham_dinh_tin_dung");
            entity.HasKey(e => e.MaThamDinh);
            entity.Property(e => e.MaThamDinh).HasColumnName("ma_tham_dinh");
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so").IsRequired();
            entity.Property(e => e.MaCanBoThamDinh).HasColumnName("ma_can_bo_tham_dinh");
            entity.Property(e => e.ThuNhapXacMinh).HasColumnName("thu_nhap_xac_minh").HasPrecision(18, 2);
            entity.Property(e => e.ChiPhiHangThang).HasColumnName("chi_phi_hang_thang").HasPrecision(18, 2);
            entity.Property(e => e.TyLeNoThuNhap).HasColumnName("ty_le_no_thu_nhap").HasPrecision(6, 4);
            entity.Property(e => e.DiemThuNhap).HasColumnName("diem_thu_nhap").HasPrecision(5, 2);
            entity.Property(e => e.DiemTaiSanBaoDam).HasColumnName("diem_tai_san_bao_dam").HasPrecision(5, 2);
            entity.Property(e => e.DiemCic).HasColumnName("diem_cic").HasPrecision(5, 2);
            entity.Property(e => e.NgayTraCuuCic).HasColumnName("ngay_tra_cuu_cic");
            entity.Property(e => e.DiemRuiRo).HasColumnName("diem_rui_ro").HasPrecision(5, 2);
            entity.Property(e => e.KhuyenNgh).HasColumnName("khuyen_nghi").HasMaxLength(30);
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.NgayThamDinh).HasColumnName("ngay_tham_dinh");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasOne(e => e.HoSoVay).WithOne(h => h.ThamDinhTinDung).HasForeignKey<ThamDinhTinDung>(e => e.MaHoSo);
            entity.HasOne(e => e.CanBoThamDinh).WithMany().HasForeignKey(e => e.MaCanBoThamDinh);
        });

        // PheDuyet
        modelBuilder.Entity<PheDuyet>(entity =>
        {
            entity.ToTable("phe_duyet");
            entity.HasKey(e => e.MaPheDuyet);
            entity.Property(e => e.MaPheDuyet).HasColumnName("ma_phe_duyet");
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so");
            entity.Property(e => e.MaNguoiPheDuyet).HasColumnName("ma_nguoi_phe_duyet");
            entity.Property(e => e.CapPheDuyet).HasColumnName("cap_phe_duyet");
            entity.Property(e => e.QuyetDinh).HasColumnName("quyet_dinh").HasMaxLength(20).IsRequired();
            entity.Property(e => e.SoTienDuocDuyet).HasColumnName("so_tien_duoc_duyet").HasPrecision(18, 2);
            entity.Property(e => e.LaiSuatDuocDuyet).HasColumnName("lai_suat_duoc_duyet").HasPrecision(6, 4);
            entity.Property(e => e.DieuKienPheDuyet).HasColumnName("dieu_kien_phe_duyet");
            entity.Property(e => e.LyDoTuChoi).HasColumnName("ly_do_tu_choi");
            entity.Property(e => e.NgayPheDuyet).HasColumnName("ngay_phe_duyet");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasOne(e => e.HoSoVay).WithOne(h => h.PheDuyet).HasForeignKey<PheDuyet>(e => e.MaHoSo);
            entity.HasOne(e => e.NguoiPheDuyet).WithMany().HasForeignKey(e => e.MaNguoiPheDuyet);
        });

        // HopDongVay
        modelBuilder.Entity<HopDongVay>(entity =>
        {
            entity.ToTable("hop_dong_vay");
            entity.HasKey(e => e.MaHopDong);
            entity.Property(e => e.MaHopDong).HasColumnName("ma_hop_dong");
            entity.Property(e => e.SoHopDong).HasColumnName("so_hop_dong").HasMaxLength(50).IsRequired();
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so");
            entity.Property(e => e.TienGoc).HasColumnName("tien_goc").HasPrecision(18, 2);
            entity.Property(e => e.LaiSuat).HasColumnName("lai_suat").HasPrecision(6, 4);
            entity.Property(e => e.NgayBatDau).HasColumnName("ngay_bat_dau");
            entity.Property(e => e.NgayDaoHan).HasColumnName("ngay_dao_han");
            entity.Property(e => e.NgayGiaiNgan).HasColumnName("ngay_giai_ngan");
            entity.Property(e => e.TongPhaiTra).HasColumnName("tong_phai_tra").HasPrecision(18, 2);
            entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(20);
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasIndex(e => e.SoHopDong).IsUnique();
            entity.HasOne(e => e.HoSoVay).WithOne(h => h.HopDongVay).HasForeignKey<HopDongVay>(e => e.MaHoSo);
        });

        // LichTraNo
        modelBuilder.Entity<LichTraNo>(entity =>
        {
            entity.ToTable("lich_tra_no");
            entity.HasKey(e => e.MaLichTraNo);
            entity.Property(e => e.MaLichTraNo).HasColumnName("ma_lich_tra_no");
            entity.Property(e => e.MaHopDong).HasColumnName("ma_hop_dong");
            entity.Property(e => e.KyThanhToan).HasColumnName("ky_thanh_toan");
            entity.Property(e => e.NgayDenHan).HasColumnName("ngay_den_han");
            entity.Property(e => e.GocPhaiTra).HasColumnName("goc_phai_tra").HasPrecision(18, 2);
            entity.Property(e => e.LaiPhaiTra).HasColumnName("lai_phai_tra").HasPrecision(18, 2);
            entity.Property(e => e.PhiPhat).HasColumnName("phi_phat").HasPrecision(18, 2);
            entity.Property(e => e.TrangThai).HasColumnName("trang_thai").HasMaxLength(30);
            entity.Property(e => e.HanThanhToan).HasColumnName("han_thanh_toan");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");

            entity.HasOne(e => e.HopDongVay).WithMany(h => h.LichTraNos).HasForeignKey(e => e.MaHopDong);
        });

        // ThanhToan
        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.ToTable("thanh_toan");
            entity.HasKey(e => e.MaThanhToan);
            entity.Property(e => e.MaThanhToan).HasColumnName("ma_thanh_toan");
            entity.Property(e => e.MaLichTraNo).HasColumnName("ma_lich_tra_no");
            entity.Property(e => e.MaHopDong).HasColumnName("ma_hop_dong");
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so");
            entity.Property(e => e.SoTienDaTra).HasColumnName("so_tien_da_tra").HasPrecision(18, 2);
            entity.Property(e => e.SoTienApGoc).HasColumnName("so_tien_ap_goc").HasPrecision(18, 2);
            entity.Property(e => e.SoTienApLai).HasColumnName("so_tien_ap_lai").HasPrecision(18, 2);
            entity.Property(e => e.SoTienApPhi).HasColumnName("so_tien_ap_phi").HasPrecision(18, 2);
            entity.Property(e => e.NgayThanhToan).HasColumnName("ngay_thanh_toan");
            entity.Property(e => e.PhuongThucThanhToan).HasColumnName("phuong_thuc_thanh_toan").HasMaxLength(30);
            entity.Property(e => e.MaGiaoDich).HasColumnName("ma_giao_dich").HasMaxLength(100);
            entity.Property(e => e.KenhThanhToan).HasColumnName("kenh_thanh_toan").HasMaxLength(100);
            entity.Property(e => e.NguoiThu).HasColumnName("nguoi_thu");
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasOne(e => e.LichTraNo).WithMany(l => l.ThanhToans).HasForeignKey(e => e.MaLichTraNo);
            entity.HasOne(e => e.HopDongVay).WithMany().HasForeignKey(e => e.MaHopDong);
            entity.HasOne(e => e.HoSoVay).WithMany().HasForeignKey(e => e.MaHoSo);
        });

        // ThongBao
        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.ToTable("thong_bao");
            entity.HasKey(e => e.MaThongBao);
            entity.Property(e => e.MaThongBao).HasColumnName("ma_thong_bao");
            entity.Property(e => e.MaNguoiNhan).HasColumnName("ma_nguoi_nhan");
            entity.Property(e => e.LoaiNguoiNhan).HasColumnName("loai_nguoi_nhan").HasMaxLength(20);
            entity.Property(e => e.TieuDe).HasColumnName("tieu_de").HasMaxLength(255).IsRequired();
            entity.Property(e => e.NoiDung).HasColumnName("noi_dung").IsRequired();
            entity.Property(e => e.Loai).HasColumnName("loai").HasMaxLength(10);
            entity.Property(e => e.DaDoc).HasColumnName("da_doc");
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so");
            entity.Property(e => e.NgayGui).HasColumnName("ngay_gui");
        });

        // NhatKyHeThong
        modelBuilder.Entity<NhatKyHeThong>(entity =>
        {
            entity.ToTable("nhat_ky_he_thong");
            entity.HasKey(e => e.MaNhatKy);
            entity.Property(e => e.MaNhatKy).HasColumnName("ma_nhat_ky");
            entity.Property(e => e.MaNguoiDung).HasColumnName("ma_nguoi_dung");
            entity.Property(e => e.HanhDong).HasColumnName("hanh_dong").HasMaxLength(50).IsRequired();
            entity.Property(e => e.TenThucThe).HasColumnName("ten_thuc_the").HasMaxLength(80).IsRequired();
            entity.Property(e => e.MaThucThe).HasColumnName("ma_thuc_the").HasMaxLength(80).IsRequired();
            entity.Property(e => e.GiaTriCu).HasColumnName("gia_tri_cu");
            entity.Property(e => e.GiaTriMoi).HasColumnName("gia_tri_moi");
            entity.Property(e => e.TruongThayDoi).HasColumnName("truong_thay_doi");
            entity.Property(e => e.IpNguon).HasColumnName("ip_nguon").HasMaxLength(45);
            entity.Property(e => e.TrinhDuyet).HasColumnName("trinh_duyet");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
        });

        // TaiSanDamBao
        modelBuilder.Entity<TaiSanDamBao>(entity =>
        {
            entity.ToTable("tai_san_dam_bao");
            entity.HasKey(e => e.MaTaiSan);
            entity.Property(e => e.MaTaiSan).HasColumnName("ma_tai_san");
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so");
            entity.Property(e => e.LoaiTaiSan).HasColumnName("loai_tai_san").HasMaxLength(50).IsRequired();
            // DB (QuanLyTinDung) KHÔNG có cột chu_so_huu → giữ property cho DTO/UI
            // nhưng không persist. Cột "mo_ta" trong DB bỏ trống, không map.
            entity.Ignore(e => e.ChuSoHuu);
            // Map vào các CỘT THẬT trong DB (trước map sai → invalid column name)
            entity.Property(e => e.SoGiayChungNhan).HasColumnName("so_chung_nhan_sh").HasMaxLength(100);
            entity.Property(e => e.DiaChiTaiSan).HasColumnName("dia_chi_tai_san");
            entity.Property(e => e.GiaTriUocTinh).HasColumnName("gia_tri_uoc_tinh").HasPrecision(18, 2);
            entity.Property(e => e.TyLeVayGiaTriTS).HasColumnName("ty_le_vay_gia_tri_ts").HasPrecision(5, 4);
            entity.Property(e => e.TinhTrangPhapLy).HasColumnName("tinh_trang_phap_ly").HasMaxLength(100);
            entity.Property(e => e.NgayDinhGia).HasColumnName("ngay_dinh_gia");
            entity.Property(e => e.DonViDinhGia).HasColumnName("don_vi_dinh_gia").HasMaxLength(255);
            entity.Property(e => e.SoHopDongBaoHiem).HasColumnName("so_hop_dong_bh").HasMaxLength(100);
            entity.Property(e => e.NgayHetHanBaoHiem).HasColumnName("ngay_het_han_bh");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasOne(e => e.HoSoVay).WithMany().HasForeignKey(e => e.MaHoSo).OnDelete(DeleteBehavior.Restrict);
        });

        // TaiLieuVay
        modelBuilder.Entity<TaiLieuVay>(entity =>
        {
            entity.ToTable("tai_lieu_vay");
            entity.HasKey(e => e.MaTaiLieu);
            entity.Property(e => e.MaTaiLieu).HasColumnName("ma_tai_lieu");
            entity.Property(e => e.MaHoSo).HasColumnName("ma_ho_so");
            entity.Property(e => e.LoaiTaiLieu).HasColumnName("loai_tai_lieu").HasMaxLength(50).IsRequired();
            entity.Property(e => e.TenTapTin).HasColumnName("ten_tap_tin").HasMaxLength(255).IsRequired();
            entity.Property(e => e.DuongDanTapTin).HasColumnName("duong_dan_tap_tin").IsRequired();
            entity.Property(e => e.PhienBan).HasColumnName("phien_ban");
            entity.Property(e => e.TrangThaiXacMinh).HasColumnName("trang_thai_xac_minh").HasMaxLength(20);
            entity.Property(e => e.MaNguoiUpload).HasColumnName("nguoi_tai_len");
            entity.Property(e => e.GhiChu).HasColumnName("ghi_chu");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");

            entity.HasOne(e => e.HoSoVay).WithMany().HasForeignKey(e => e.MaHoSo).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.NguoiUpload).WithMany().HasForeignKey(e => e.MaNguoiUpload).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
