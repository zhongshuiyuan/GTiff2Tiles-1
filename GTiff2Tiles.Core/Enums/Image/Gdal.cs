﻿namespace GTiff2Tiles.Core.Enums.Image
{
    /// <summary>
    /// Static class with Gdal constants.
    /// </summary>
    public static class Gdal
    {
        #region Proj4

        /// <summary>
        /// +proj=longlat
        /// </summary>
        public const string LongLat = "+proj=longlat";

        /// <summary>
        /// +datum=WGS84
        /// </summary>
        public const string Wgs84 = "+datum=WGS84";

        #endregion

        #region GdalInfo

        /// <summary>
        /// Input image is tiled.
        /// </summary>
        public const string Block = "Block";

        /// <summary>
        /// Check type.
        /// </summary>
        public const string Byte = "Byte";

        #endregion

        #region GdalWarp

        /// <summary>
        /// Options for GdalWarp.
        /// </summary>
        public static readonly string[] RepairTifOptions =
        {
            "-overwrite", "-t_srs", "EPSG:4326", "-co", "TILED=YES", "-multi", "-srcnodata", "0",
        };

        /// <summary>
        /// Name for temporary (converted) file.
        /// </summary>
        public const string TempFileName = "Temp";

        #endregion
    }
}
