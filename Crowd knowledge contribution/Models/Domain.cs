using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Crowd_knowledge_contribution.Models
{
    public class Domain
    {
        [Key]
        public int DomainId { get; set; }

        public string DomainName { get; set; }

        public virtual ICollection<Article> Articles { get; set; }
    }
}