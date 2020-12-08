using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Crowd_knowledge_contribution.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public int ArticleId { get; set; }
        //public int VersionId { get; set; }

        public virtual Article Article { get; set; }
    }
}