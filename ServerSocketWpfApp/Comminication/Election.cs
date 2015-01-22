using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSocketWpfApp.Comminication
{
    public class Election
    {
        public String type { get; set; }
        public List<Member> members = new List<Member>();
        public Election(String _type, List<Member> _list)
        {
            type = _type;
            members = _list;
        }
    }
    public class Member
    {
        public String ip { get; set; }

        public int elNo { get; set; }

        public bool received = false;

        public Member(String _ip, int _elNo)
        {
            ip = _ip;
            elNo = _elNo;
        }
    }
}
