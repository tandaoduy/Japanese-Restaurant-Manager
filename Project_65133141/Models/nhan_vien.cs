// Partial class extension for NhanVien to support snake_case property names
namespace Project_65133141.Models
{
    public partial class NhanVien
    {
        // Property aliases for snake_case compatibility
        public long id 
        { 
            get { return this.NhanVienID; } 
            set { this.NhanVienID = value; } 
        }
        
        public long vai_tro_id 
        { 
            get { return this.VaiTroID; } 
            set { this.VaiTroID = value; } 
        }
        
        public string ho_ten 
        { 
            get { return this.HoTen; } 
            set { this.HoTen = value; } 
        }
        
        public string email 
        { 
            get { return this.Email; } 
            set { this.Email = value; } 
        }
        
        public string so_dien_thoai 
        { 
            get { return this.SDT; } 
            set { this.SDT = value; } 
        }
        
        public string mat_khau 
        { 
            get { return this.MatKhau; } 
            set { this.MatKhau = value; } 
        }
        
        // Note: chi_nhanh_id and luong are not in the database model
        // These are kept for backward compatibility but will not be persisted
        public long? chi_nhanh_id { get; set; }
        public decimal? luong { get; set; }
        
        public System.DateTime? ngay_vao_lam 
        { 
            get { return this.NgayVaoLam; } 
            set { this.NgayVaoLam = value; } 
        }
        
        public string trang_thai 
        { 
            get { return this.TrangThai; } 
            set { this.TrangThai = value; } 
        }
        
        // Navigation property alias for snake_case compatibility
        public VaiTro vai_tro 
        { 
            get { return this.VaiTro; } 
            set { this.VaiTro = value; } 
        }
    }
}

