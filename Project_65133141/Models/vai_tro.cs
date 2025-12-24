// Partial class extension for VaiTro to support snake_case property names
namespace Project_65133141.Models
{
    public partial class VaiTro
    {
        // Property aliases for snake_case compatibility
        public long id 
        { 
            get { return this.VaiTroID; } 
            set { this.VaiTroID = value; } 
        }
        
        public string ten_vai_tro 
        { 
            get { return this.TenVaiTro; } 
            set { this.TenVaiTro = value; } 
        }
    }
}

