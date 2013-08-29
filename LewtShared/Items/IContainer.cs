using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lewt.Shared.Items
{
    public interface IContainer
    {
        Inventory Inventory
        {
            get;
            set;
        }
    }
}
