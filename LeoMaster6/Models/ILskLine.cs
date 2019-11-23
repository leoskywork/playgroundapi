using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeoMaster6.Models
{
    public interface ILskLine
    {
    }

    public interface ILskjsonLine : ILskLine
    {
        Guid Uid { get; }
        bool? IsDeleted { get; }
    }
}
