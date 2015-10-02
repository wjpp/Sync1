using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Model
{
    class LoginInfo
    {
        public string Error { get; set; }
        public UserInfo User { get; set; }
        public List<CompanyInfo> Companies { get; set; }
    }
}
