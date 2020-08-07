using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeekaRepo.Entities
{
    public class BranchHierarchy
    {
        public string Name { get; set; }
        public BranchHierarchy Parent { get; set; }
        public List<BranchHierarchy> Children { get; set; } = new List<BranchHierarchy>();
    }
}
