using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Domain.Entities
{
    public enum ChannelType { Text, Voice, Announcement }

    public class Channel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServerId { get; set; }
        public string Name { get; set; }
        public ChannelType Type { get; set; } = ChannelType.Text;
        public int Position { get; set; }
        public Server Server { get; set; }
        public ICollection<Message> Messages { get; set; }
    }


}
