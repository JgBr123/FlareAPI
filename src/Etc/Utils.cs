namespace FlareAPI
{
    public class Utils
    {
        //
        // PUBLIC FUNCTIONS
        //

        /// <summary>
        /// Checks if a byte range is valid.
        /// </summary>
        /// <param name="range">The range of the file, in bytes, following the HTTP standard.</param>
        /// <param name="fileSize">The size of the file, in bytes, that this range refers to.</param>
        /// <returns><see langword="true"/> if the range is valid, otherwise <see langword="false"/>.</returns>
        public static bool IsValidRange(string range, long fileSize)
        {
            try
            {
                var pr = ParseRange(range);

                if (pr.startingRange < 0) return false; //If starting range is negatuve, return false
                if (pr.endingRange != null && pr.endingRange < 0) return false; //If ending range is negatuve, return false

                if (pr.endingRange != null && pr.endingRange >= fileSize) return false; //If ending range is bigger than the filesize, return false
                if (pr.startingRange >= fileSize) return false; //If starting range is bigger than the filesize, return false

                if (pr.endingRange != null && pr.startingRange > pr.endingRange) return false; //If starting range is bigger than the ending range, return false

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Calculates the length of a byte range.
        /// </summary>
        /// <param name="range">The range of the file, in bytes, following the HTTP standard.</param>
        /// <param name="fileSize">The size of the file, in bytes, that this range refers to.</param>
        /// <returns>The length, in bytes.</returns>
        public static long CalculateRangeLength(string range, long fileSize)
        {
            var pr = ParseRange(range);

            if (pr.startingRange != null)
            {
                if (pr.endingRange != null)
                {
                    return pr.endingRange.Value - pr.startingRange.Value + 1; //Returns the length of the range of data
                }
                else return fileSize - pr.startingRange.Value;  //Returns the length of the file beginning in the starting point
            }
            else return fileSize; //Returns full filesize if there is no starting range
        }

        //
        // INTERNAL FUNCTIONS
        //

        internal static (long? startingRange, long? endingRange) ParseRange(string range)
        {
            long? startingRange = null;
            long? endingRange = null;

            if (range.StartsWith("bytes="))
            {
                var parsedRange = range.Substring(6).Split('-');
                if (parsedRange.Length == 2)
                {
                    if (Int64.TryParse(parsedRange[0], out long _startingRange)) startingRange = _startingRange;
                    else throw new InvalidContentRangeException();

                    if (String.IsNullOrEmpty(parsedRange[1])) endingRange = null;
                    else if (Int64.TryParse(parsedRange[1], out long _endingRange)) endingRange = _endingRange;
                    else throw new InvalidContentRangeException();
                }
                else throw new InvalidContentRangeException();
            }
            else throw new InvalidContentRangeException();

            return (startingRange, endingRange);
        }
    }
}
