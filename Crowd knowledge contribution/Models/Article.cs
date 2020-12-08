using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace Crowd_knowledge_contribution.Models
{
    public class Article
    {
        [Key, Column(Order = 0)] 
        public int ArticleId { get; set; }
        [Key, Column(Order = 1)] 
        public int VersionId { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(20, ErrorMessage = "Titlul nu poate avea mai mult de 20 caractere")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Continutul articolului este obligatoriu")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; }
        public DateTime LastModified { get; set; }

        [Required(ErrorMessage = "Domeniul este obligatoriu bro")]
        public int DomainId { get; set; }
        public virtual Domain Domain { get; set; }

        public IEnumerable<SelectListItem> Dom { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
    }
}