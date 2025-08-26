namespace OceanView.Domain.Models
{
    public class SavedSearchResult
    {
        public List<HotelInfo> Hotels { get; set; } = [];

        public bool IsSearchCompleted { get; set; }

        public bool IsSearchInterrupted { get; set; }
    }
}
