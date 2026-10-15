using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class CommerceQuarantine
    {
        public int Id { get; set; }
        public string PcProcessDate { get; set; }
        public string PcNomComRed { get; set; }
        public string PcNumDoc { get; set; }
        public string Motivo { get; set; }
    }
}
