namespace Library.Infrastructure.Models
{
    // читацький квиток, зв'язок один до одного з ReaderModel
    public class LibraryCardModel : BaseModel
    {
        public string CardNumber { get; set; } = "";
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public int ReaderId { get; set; }
        public ReaderModel? Reader { get; set; }
    }
}
