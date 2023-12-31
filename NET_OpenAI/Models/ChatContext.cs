using Microsoft.EntityFrameworkCore;
using NET_OpenAI.Controllers;

namespace NET_OpenAI.Models
{
    public class ChatContext : DbContext
    {
        public ChatContext(DbContextOptions<ChatContext> options)
            : base(options)
        { }

        public DbSet<ChatMessage> ChatMessages { get; set; }
    }
}

