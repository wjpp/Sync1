using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Model
{
    class UserInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public long Id { get; set; }
        public bool Online { get; set; }
        public string ProfileImgURL { get; set; }
        public string ApiKey { get; set; }
    }
}
