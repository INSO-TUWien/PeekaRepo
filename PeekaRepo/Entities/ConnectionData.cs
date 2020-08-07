using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeekaRepo.Entities
{
    public class ConnectionData
    {
        public string Owner { get; set; } = "steinzer";
        public string Token { get; set; } = "";
        public string Repository { get; set; } = "BranchVisualizationTest";
        public string Username { get; set; }
        public string Password { get; set; }
    }
}