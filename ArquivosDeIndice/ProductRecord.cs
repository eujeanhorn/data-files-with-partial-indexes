using System;

namespace ArquivosDeIndice
{
    internal class ProductRecord
    {
        public int ProductId { get; set; }
        public long CategoryId { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public double Price { get; set; }
        public byte Flags { get; set; }
        public long Continuacao { get; set; }
    }
}