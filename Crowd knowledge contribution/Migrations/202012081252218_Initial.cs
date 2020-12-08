namespace Crowd_knowledge_contribution.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Articles",
                c => new
                    {
                        ArticleId = c.Int(nullable: false),
                        VersionId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 20),
                        Content = c.String(nullable: false),
                        LastModified = c.DateTime(nullable: false),
                        DomainId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ArticleId, t.VersionId })
                .ForeignKey("dbo.Domains", t => t.DomainId, cascadeDelete: true)
                .Index(t => t.DomainId);
            
            CreateTable(
                "dbo.Comments",
                c => new
                    {
                        CommentId = c.Int(nullable: false, identity: true),
                        Content = c.String(nullable: false),
                        Date = c.DateTime(nullable: false),
                        ArticleId = c.Int(nullable: false),
                        Article_ArticleId = c.Int(),
                        Article_VersionId = c.Int(),
                    })
                .PrimaryKey(t => t.CommentId)
                .ForeignKey("dbo.Articles", t => new { t.Article_ArticleId, t.Article_VersionId })
                .Index(t => new { t.Article_ArticleId, t.Article_VersionId });
            
            CreateTable(
                "dbo.Domains",
                c => new
                    {
                        DomainId = c.Int(nullable: false, identity: true),
                        DomainName = c.String(),
                    })
                .PrimaryKey(t => t.DomainId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Articles", "DomainId", "dbo.Domains");
            DropForeignKey("dbo.Comments", new[] { "Article_ArticleId", "Article_VersionId" }, "dbo.Articles");
            DropIndex("dbo.Comments", new[] { "Article_ArticleId", "Article_VersionId" });
            DropIndex("dbo.Articles", new[] { "DomainId" });
            DropTable("dbo.Domains");
            DropTable("dbo.Comments");
            DropTable("dbo.Articles");
        }
    }
}
