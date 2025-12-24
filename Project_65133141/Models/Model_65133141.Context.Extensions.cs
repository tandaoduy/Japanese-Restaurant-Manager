// Extension for QuanLyNhaHangNhat_65133141Entities4 to support snake_case property names
using System.Data.Entity;

namespace Project_65133141.Models
{
    public partial class QuanLyNhaHangNhat_65133141Entities6
    {
        // Alias for NhanViens to support snake_case naming
        public DbSet<NhanVien> nhan_vien
        {
            get { return this.NhanViens; }
            set { this.NhanViens = value; }
        }
        
        // Alias for VaiTroes to support snake_case naming  
        public DbSet<VaiTro> vai_tro
        {
            get { return this.VaiTroes; }
            set { this.VaiTroes = value; }
        }
        
        // Alias for Users to support khach_hang naming (assuming khach_hang maps to User)
        public DbSet<User> khach_hang
        {
            get { return this.Users; }
            set { this.Users = value; }
        }
    }
}
