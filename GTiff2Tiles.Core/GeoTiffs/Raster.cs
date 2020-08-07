﻿#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA1062 // Check args

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using GTiff2Tiles.Core.Constants;
using GTiff2Tiles.Core.Coordinates;
using GTiff2Tiles.Core.Enums;
using GTiff2Tiles.Core.Exceptions;
using GTiff2Tiles.Core.Helpers;
using GTiff2Tiles.Core.Images;
using GTiff2Tiles.Core.Tiles;
using NetVips;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace GTiff2Tiles.Core.GeoTiffs
{
    /// <summary>
    /// Class for creating <see cref="RasterTile"/>s
    /// </summary>
    public class Raster : IGeoTiff
    {
        #region Properties

        #region Private

        /// <summary>
        /// Name for temporary file
        /// <para/>see <see cref="Raster(IEnumerable{byte}, CoordinateSystem)"/>
        /// and <see cref="Raster(Stream, CoordinateSystem)"/>
        /// </summary>
        private readonly string _tempFileName = $"{DateTime.Now.ToString(DateTimePatterns.LongWithMs, CultureInfo.InvariantCulture)}_tmp.tif";

        /// <summary>
        /// This <see cref="Raster"/>'s data
        /// </summary>
        private Image Data { get; }

        #endregion

        #region Public

        /// <inheritdoc />
        public Size Size { get; }

        /// <inheritdoc />
        public GeoCoordinate MinCoordinate { get; }

        /// <inheritdoc />
        public GeoCoordinate MaxCoordinate { get; }

        /// <inheritdoc />
        public CoordinateSystem GeoCoordinateSystem { get; }

        /// <inheritdoc />
        public bool IsDisposed { get; private set; }

        #endregion

        #endregion

        #region Constructor/Destructor

        /// <summary>
        /// Creates new <see cref="Raster"/> object
        /// </summary>
        /// <param name="inputFilePath">Input GeoTiff's path</param>
        /// <param name="coordinateSystem">Desired coordinate system</param>
        /// <param name="maxMemoryCache">Max size of input image to store in RAM
        /// <remarks><para/>2GB by default</remarks></param>
        public Raster(string inputFilePath, CoordinateSystem coordinateSystem, long maxMemoryCache = 2147483648)
        {
            // Disable NetVips warnings for tiff
            NetVipsHelper.DisableLog();

            #region Check parameters

            if (!CheckHelper.CheckFile(inputFilePath, true)) throw new RasterException();

            #endregion

            bool memory = new FileInfo(inputFilePath).Length <= maxMemoryCache;
            Data = Image.NewFromFile(inputFilePath, memory, NetVips.Enums.Access.Random);

            // Get border coordinates и raster sizes
            Size = new Size(Data.Width, Data.Height);

            GeoCoordinateSystem = coordinateSystem;
            (MinCoordinate, MaxCoordinate) = GdalWorker.GetImageBorders(inputFilePath, Size, GeoCoordinateSystem);
        }

        /// <inheritdoc cref="Raster(string,CoordinateSystem,long)"/>
        /// <param name="inputBytes"><see cref="IEnumerable{T}"/> of GeoTiff's <see cref="byte"/>s</param>
        /// <param name="coordinateSystem"></param>
        public Raster(IEnumerable<byte> inputBytes, CoordinateSystem coordinateSystem)
        {
            // Disable NetVips warnings for tiff
            NetVipsHelper.DisableLog();

            Data = Image.NewFromBuffer(inputBytes.ToArray(), access: NetVips.Enums.Access.Random);

            // Get border coordinates и raster sizes
            Size = new Size(Data.Width, Data.Height);

            GeoCoordinateSystem = coordinateSystem;

            // TODO: get coordinates without fileinfo
            FileInfo inputFileInfo = new FileInfo(_tempFileName);
            Data.WriteToFile(inputFileInfo.FullName);
            (MinCoordinate, MaxCoordinate) = GdalWorker.GetImageBorders(inputFileInfo.FullName, Size, GeoCoordinateSystem);
            inputFileInfo.Delete();
        }

        /// <inheritdoc cref="Raster(string,CoordinateSystem,long)"/>
        /// <param name="inputStream"><see cref="Stream"/> with GeoTiff</param>
        /// <param name="coordinateSystem"></param>
        public Raster(Stream inputStream, CoordinateSystem coordinateSystem)
        {
            // Disable NetVips warnings for tiff
            NetVipsHelper.DisableLog();

            Data = Image.NewFromStream(inputStream, access: NetVips.Enums.Access.Random);

            // Get border coordinates и raster sizes
            Size = new Size(Data.Width, Data.Height);

            GeoCoordinateSystem = coordinateSystem;

            // TODO: get coordinates without fileinfo
            FileInfo inputFileInfo = new FileInfo(_tempFileName);
            Data.WriteToFile(inputFileInfo.FullName);
            (MinCoordinate, MaxCoordinate) = GdalWorker.GetImageBorders(inputFileInfo.FullName, Size, GeoCoordinateSystem);
            inputFileInfo.Delete();
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~Raster() => Dispose(false);

        #endregion

        #region Methods

        #region Dispose

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="Dispose()"/>
        /// <param name="disposing">Dispose static fields?</param>
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                //Occurs only if called by programmer. Dispose static things here.
            }

            Data.Dispose();

            IsDisposed = true;
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            try
            {
                Dispose();

                return default;
            }
            catch (Exception exception)
            {
                //Weird issue -- Doesn't work in CI
                //return ValueTask.FromException(exception);
                return new ValueTask(Task.FromException(exception));
            }
        }

        #endregion

        #region Create tile image

        /// <summary>
        /// Create <see cref="Image"/> for one <see cref="RasterTile"/>
        /// from input <see cref="Image"/> or tile cache
        /// </summary>
        /// <param name="tileCache">Source <see cref="Image"/>
        /// or tile cache</param>
        /// <param name="tile">Target <see cref="RasterTile"/></param>
        /// <param name="interpolation">Interpolation of ready tiles</param>
        /// <returns>Ready <see cref="Image"/> for <see cref="RasterTile"/></returns>
        public Image CreateTileImage(Image tileCache, RasterTile tile, string interpolation)
        {
            if (tileCache == null) throw new ArgumentNullException(nameof(tileCache));
            if (tile == null) throw new ArgumentNullException(nameof(tile));

            // Get postitions and sizes for current tile
            (Area readArea, Area writeArea) = Area.GetAreas(this, tile);

            // Scaling calculations
            double xScale = (double)writeArea.Size.Width / readArea.Size.Width;
            double yScale = (double)writeArea.Size.Height / readArea.Size.Height;

            // Crop and resize tile
            Image tempTileImage = tileCache.Crop((int)readArea.OriginCoordinate.X, (int)readArea.OriginCoordinate.Y,
                                                 readArea.Size.Width, readArea.Size.Height)
                                           .Resize(xScale, interpolation, yScale);

            // Add alpha channel if needed
            Band.AddDefaultBands(ref tempTileImage, tile.BandsCount);

            // Make transparent image and insert tile
            return Image.Black(tile.Size.Width, tile.Size.Height).NewFromImage(0, 0, 0, 0)
                        .Insert(tempTileImage, (int)writeArea.OriginCoordinate.X,
                                (int)writeArea.OriginCoordinate.Y);
        }

        #endregion

        #region WriteTile

        /// <summary>
        /// Gets data from source <see cref="Image"/>
        /// or tile cache for specified <see cref="RasterTile"/>
        /// and writes it to ready file
        /// </summary>
        /// <param name="tileCache">Source <see cref="Image"/>
        /// or tile cache</param>
        /// <param name="tile">Target <see cref="RasterTile"/></param>
        /// <param name="interpolation">Interpolation of ready tiles</param>
        public void WriteTileToFile(Image tileCache, RasterTile tile, string interpolation)
        {
            using Image tileImage = CreateTileImage(tileCache, tile, interpolation);

            //TODO: validate size before writing
            tileImage.WriteToFile(tile.Path);
        }

        /// <inheritdoc cref="WriteTileToFile"/>
        public Task WriteTileToFileAsync(Image tileCache, RasterTile tile, string interpolation) =>
            Task.Run(() => WriteTileToFile(tileCache, tile, interpolation));

        /// <summary>
        /// Gets data from source <see cref="Image"/>
        /// or tile cache for specified <see cref="RasterTile"/>
        /// and writes it to <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <param name="tileCache">Source <see cref="Image"/>
        /// or tile cache</param>
        /// <param name="tile">Target <see cref="RasterTile"/></param>
        /// <param name="interpolation">Interpolation of ready tiles</param>
        /// <returns><see cref="RasterTile"/>'s <see cref="byte"/>s</returns>
        public IEnumerable<byte> WriteTileToEnumerable(Image tileCache, RasterTile tile,
                                                       string interpolation)
        {
            using Image tileImage = CreateTileImage(tileCache, tile, interpolation);

            //TODO: validate size before writing
            return tileImage.WriteToMemory();
        }

        /// <summary>
        /// Gets data from source <see cref="Image"/>
        /// or tile cache for specified <see cref="RasterTile"/>
        /// and writes it to <see cref="ChannelWriter{T}"/>
        /// </summary>
        /// <param name="tileCache">Source <see cref="Image"/>
        /// or tile cache</param>
        /// <param name="tile">Target <see cref="RasterTile"/></param>
        /// <param name="channelWriter">Target <see cref="ChannelWriter{T}"/></param>
        /// <param name="interpolation">Interpolation of ready tiles</param>
        /// <returns><see langword="true"/> if <see cref="ITile"/> was written;
        /// <see langword="false"/> otherwise</returns>
        public bool WriteTileToChannel(Image tileCache, RasterTile tile, ChannelWriter<ITile> channelWriter,
                                       string interpolation)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile));
            if (channelWriter == null) throw new ArgumentNullException(nameof(channelWriter));

            tile.Bytes = WriteTileToEnumerable(tileCache, tile, interpolation);

            return tile.Validate(false) && channelWriter.TryWrite(tile);
        }

        /// <returns></returns>
        /// <inheritdoc cref="WriteTileToChannel"/>
        public ValueTask WriteTileToChannelAsync(Image tileCache, RasterTile tile,
                                                 ChannelWriter<ITile> channelWriter, string interpolation)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile));
            if (channelWriter == null) throw new ArgumentNullException(nameof(channelWriter));

            tile.Bytes = WriteTileToEnumerable(tileCache, tile, interpolation);

            return !tile.Validate(false) ? ValueTask.CompletedTask : channelWriter.WriteAsync(tile);
        }

        #endregion

        #region WriteTiles

        /// <inheritdoc cref="WriteTilesToDirectoryAsync"/>
        public void WriteTilesToDirectory(string outputDirectoryPath, int minZ, int maxZ,
                                          bool tmsCompatible = false, Size tileSize = null,
                                          TileExtension tileExtension = TileExtension.Png,
                                          string interpolation = Interpolations.Lanczos3,
                                          int bandsCount = RasterTile.DefaultBandsCount,
                                          int tileCacheCount = 1000, int threadsCount = 0,
                                          IProgress<double> progress = null,
                                          bool isPrintEstimatedTime = false)
        {
            #region Parameters checking

            progress ??= new Progress<double>();
            tileSize ??= Tile.DefaultSize;

            ParallelOptions parallelOptions = new ParallelOptions();
            if (threadsCount > 0) parallelOptions.MaxDegreeOfParallelism = threadsCount;

            #region Progress stuff

            Stopwatch stopwatch = isPrintEstimatedTime ? Stopwatch.StartNew() : null;
            int tilesCount = Number.GetCount(MinCoordinate, MaxCoordinate, minZ, maxZ, tmsCompatible, tileSize);
            double counter = 0.0;

            if (tilesCount <= 0) return;

            #endregion

            #endregion

            // Create tile cache to read data from it
            using Image tileCache = Data.Tilecache(tileSize.Width, tileSize.Height, tileCacheCount, threaded: true);

            void MakeTile(int x, int y, int z)
            {
                // Create directories for the tile
                // The overall structure looks like: outputDirectory/zoom/x/y.png
                string tileDirectoryPath = Path.Combine(outputDirectoryPath, $"{z}", $"{x}");
                if (!CheckHelper.CheckDirectory(tileDirectoryPath)) throw new RasterException();

                Number tileNumber = new Number(x, y, z);
                RasterTile tile = new RasterTile(tileNumber, GeoCoordinateSystem, extension: tileExtension,
                                                 tmsCompatible: tmsCompatible, size: tileSize, bandsCount: bandsCount);

                // Warning: OpenLayers requires replacement of tileY to tileY+1
                tile.Path = Path.Combine(tileDirectoryPath, $"{y}{tile.GetExtensionString()}");

                // ReSharper disable once AccessToDisposedClosure
                WriteTileToFile(tileCache, tile, interpolation);

                // Report progress
                counter++;
                double percentage = counter / tilesCount * 100.0;
                progress.Report(percentage);

                // Estimated time left calculation
                ProgressHelper.PrintEstimatedTimeLeft(percentage, stopwatch);
            }

            // For each zoom
            for (int zoom = minZ; zoom <= maxZ; zoom++)
            {
                // Get tiles min/max numbers
                (Number minNumber, Number maxNumber) = GeoCoordinate.GetNumbers(MinCoordinate, MaxCoordinate,
                        zoom, tileSize.Width, tmsCompatible);

                // For each tile on given zoom calculate positions/sizes and save as file
                for (int tileY = minNumber.Y; tileY <= maxNumber.Y; tileY++)
                {
                    int y = tileY;
                    int z = zoom;

                    Parallel.For(minNumber.X, maxNumber.X + 1, parallelOptions, x => MakeTile(x, y, z));
                }
            }
        }

        /// <summary>
        /// Crops current <see cref="RasterTile"/> on <see cref="RasterTile"/>s
        /// and writes them to <paramref name="outputDirectoryPath"/>
        /// </summary>
        /// <param name="outputDirectoryPath">Directory for output <see cref="RasterTile"/>s</param>
        /// <param name="tileExtension">Extension of ready <see cref="RasterTile"/>s
        /// <remarks><para/>.png by default</remarks></param>
        /// <returns></returns>
        /// <inheritdoc cref="WriteTilesToAsyncEnumerable"/>
        /// <param name="minZ"></param>
        /// <param name="maxZ"></param>
        /// <param name="tmsCompatible"></param>
        /// <param name="tileSize"></param>
        /// <param name="interpolation"></param>
        /// <param name="bandsCount"></param>
        /// <param name="tileCacheCount"></param>
        /// <param name="threadsCount">T</param>
        /// <param name="progress"></param>
        /// <param name="isPrintEstimatedTime"></param>
        public Task WriteTilesToDirectoryAsync(string outputDirectoryPath, int minZ, int maxZ,
                                               bool tmsCompatible = false, Size tileSize = null,
                                               TileExtension tileExtension = TileExtension.Png,
                                               string interpolation = Interpolations.Lanczos3,
                                               int bandsCount = RasterTile.DefaultBandsCount,
                                               int tileCacheCount = 1000, int threadsCount = 0,
                                               IProgress<double> progress = null,
                                               bool isPrintEstimatedTime = false) =>
            Task.Run(() => WriteTilesToDirectory(outputDirectoryPath, minZ, maxZ, tmsCompatible, tileSize,
                                                 tileExtension, interpolation, bandsCount, tileCacheCount,
                                                 threadsCount, progress, isPrintEstimatedTime));

        /// <inheritdoc cref="WriteTilesToChannelAsync"/>
        public void WriteTilesToChannel(ChannelWriter<ITile> channelWriter, int minZ, int maxZ,
                                        bool tmsCompatible = false, Size tileSize = null,
                                        string interpolation = Interpolations.Lanczos3,
                                        int bandsCount = RasterTile.DefaultBandsCount,
                                        int tileCacheCount = 1000, int threadsCount = 0,
                                        IProgress<double> progress = null,
                                        bool isPrintEstimatedTime = false)
        {
            #region Parameters checking

            progress ??= new Progress<double>();
            tileSize ??= Tile.DefaultSize;

            ParallelOptions parallelOptions = new ParallelOptions();
            if (threadsCount > 0) parallelOptions.MaxDegreeOfParallelism = threadsCount;

            #region Progress stuff

            Stopwatch stopwatch = isPrintEstimatedTime ? Stopwatch.StartNew() : null;
            int tilesCount = Number.GetCount(MinCoordinate, MaxCoordinate, minZ, maxZ, tmsCompatible, tileSize);
            double counter = 0.0;

            if (tilesCount <= 0) return;

            #endregion

            #endregion

            // Create tile cache to read data from it
            using Image tileCache = Data.Tilecache(tileSize.Width, tileSize.Height, tileCacheCount, threaded: true);

            void MakeTile(int x, int y, int z)
            {
                Number tileNumber = new Number(x, y, z);
                RasterTile tile = new RasterTile(tileNumber, GeoCoordinateSystem, tmsCompatible: tmsCompatible,
                    size: tileSize, bandsCount: bandsCount);

                // ReSharper disable once AccessToDisposedClosure
                if (!WriteTileToChannel(tileCache, tile, channelWriter, interpolation)) return;

                // Report progress
                counter++;
                double percentage = counter / tilesCount * 100.0;
                progress.Report(percentage);

                // Estimated time left calculation
                ProgressHelper.PrintEstimatedTimeLeft(percentage, stopwatch);
            }

            // For each zoom
            for (int zoom = minZ; zoom <= maxZ; zoom++)
            {
                // Get tiles min/max numbers
                (Number minNumber, Number maxNumber) = GeoCoordinate.GetNumbers(MinCoordinate, MaxCoordinate,
                    zoom, tileSize.Width, tmsCompatible);

                // For each tile on given zoom calculate positions/sizes and save as file
                for (int tileY = minNumber.Y; tileY <= maxNumber.Y; tileY++)
                {
                    int y = tileY;
                    int z = zoom;

                    Parallel.For(minNumber.X, maxNumber.X + 1, parallelOptions, x => MakeTile(x, y, z));
                }
            }
        }

        /// <summary>
        /// Crops current <see cref="Raster"/> on <see cref="ITile"/>s
        /// and writes them to <paramref name="channelWriter"/>
        /// </summary>
        /// <param name="channelWriter"><see cref="Channel"/> to write <see cref="ITile"/> to</param>
        /// <returns></returns>
        /// <inheritdoc cref="WriteTilesToAsyncEnumerable"/>
        /// <param name="minZ"></param>
        /// <param name="maxZ"></param>
        /// <param name="tmsCompatible"></param>
        /// <param name="tileSize"></param>
        /// <param name="interpolation"></param>
        /// <param name="bandsCount"></param>
        /// <param name="tileCacheCount"></param>
        /// <param name="threadsCount"></param>
        /// <param name="progress"></param>
        /// <param name="isPrintEstimatedTime"></param>
        public Task WriteTilesToChannelAsync(ChannelWriter<ITile> channelWriter, int minZ, int maxZ,
                                             bool tmsCompatible = false, Size tileSize = null,
                                             string interpolation = Interpolations.Lanczos3,
                                             int bandsCount = RasterTile.DefaultBandsCount,
                                             int tileCacheCount = 1000, int threadsCount = 0,
                                             IProgress<double> progress = null,
                                             bool isPrintEstimatedTime = false) =>
            Task.Run(() => WriteTilesToChannel(channelWriter, minZ, maxZ, tmsCompatible, tileSize,
                                               interpolation, bandsCount, tileCacheCount, threadsCount,
                                               progress, isPrintEstimatedTime));

        /// <summary>
        /// Crops current <see cref="Raster"/> on <see cref="ITile"/>s
        /// and writes them to <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <returns><see cref="IEnumerable{T}"/> of <see cref="ITile"/>s</returns>
        /// <inheritdoc cref="WriteTilesToAsyncEnumerable"/>
        public IEnumerable<ITile> WriteTilesToEnumerable(int minZ, int maxZ,
                                                         bool tmsCompatible = false, Size tileSize = null,
                                                         string interpolation = Interpolations.Lanczos3,
                                                         int bandsCount = RasterTile.DefaultBandsCount,
                                                         int tileCacheCount = 1000,
                                                         IProgress<double> progress = null,
                                                         bool isPrintEstimatedTime = false)
        {
            #region Parameters checking

            progress ??= new Progress<double>();
            tileSize ??= Tile.DefaultSize;

            #region Progress stuff

            Stopwatch stopwatch = isPrintEstimatedTime ? Stopwatch.StartNew() : null;
            int tilesCount = Number.GetCount(MinCoordinate, MaxCoordinate, minZ, maxZ, tmsCompatible, tileSize);
            double counter = 0.0;

            if (tilesCount <= 0) yield break;

            #endregion

            #endregion

            // Create tile cache to read data from it
            using Image tileCache = Data.Tilecache(tileSize.Width, tileSize.Height, tileCacheCount, threaded: true);

            ITile MakeTile(int x, int y, int z)
            {
                Number tileNumber = new Number(x, y, z);
                RasterTile tile = new RasterTile(tileNumber, GeoCoordinateSystem, tmsCompatible: tmsCompatible,
                    size: tileSize, bandsCount: bandsCount);

                tile.Bytes = WriteTileToEnumerable(tileCache, tile, interpolation);

                // Report progress
                counter++;
                double percentage = counter / tilesCount * 100.0;
                progress.Report(percentage);

                // Estimated time left calculation
                ProgressHelper.PrintEstimatedTimeLeft(percentage, stopwatch);

                return tile;
            }

            // For each specified zoom
            for (int zoom = minZ; zoom <= maxZ; zoom++)
            {
                // Get tiles min/max numbers
                (Number minNumber, Number maxNumber) = GeoCoordinate.GetNumbers(MinCoordinate, MaxCoordinate,
                        zoom, tileSize.Width, tmsCompatible);

                // For each tile on given zoom calculate positions/sizes and save as file
                for (int tileY = minNumber.Y; tileY <= maxNumber.Y; tileY++)
                {
                    for (int tileX = minNumber.X; tileX <= maxNumber.X; tileX++)
                        yield return MakeTile(tileX, tileY, zoom);
                }
            }
        }

        /// <summary>
        /// Crops current <see cref="Raster"/> on <see cref="ITile"/>s
        /// and writes them to <see cref="IAsyncEnumerable{T}"/>
        /// </summary>
        /// <param name="minZ">Minimum cropped zoom</param>
        /// <param name="maxZ">Maximum cropped zoom</param>
        /// <param name="tmsCompatible">Do you want to create tms-compatible <see cref="ITile"/>s?
        /// <remarks><para/><see langword="false"/> by default</remarks></param>
        /// <param name="tileSize"><see cref="Images.Size"/> of <see cref="ITile"/>s
        /// <remarks><para/>256x256 by default</remarks></param>
        /// <param name="interpolation">Interpolation of ready tiles
        /// <remarks><para/><see cref="Interpolations.Lanczos3"/> by default</remarks></param>
        /// <param name="bandsCount">Count of <see cref="Band"/>s in ready <see cref="ITile"/>s
        /// <remarks><para/>4 by default</remarks></param>
        /// <param name="tileCacheCount">Count of <see cref="ITile"/> to be in cache
        /// <remarks><para/>1000 by default</remarks></param>
        /// <param name="threadsCount">Threads count
        /// <remarks><para/>Calculates automatically by default</remarks></param>
        /// <param name="progress">Progress-reporter
        /// <remarks><para/><see langword="null"/> by default</remarks></param>
        /// <param name="isPrintEstimatedTime">Do you want to see estimated time left?
        /// <remarks><para/><see langword="false"/> by default</remarks></param>
        /// <returns><see cref="IAsyncEnumerable{T}"/> of <see cref="ITile"/>s</returns>
        public IAsyncEnumerable<ITile> WriteTilesToAsyncEnumerable(int minZ, int maxZ,
                                                                   bool tmsCompatible = false, Size tileSize = null,
                                                                   string interpolation = Interpolations.Lanczos3,
                                                                   int bandsCount = RasterTile.DefaultBandsCount,
                                                                   int tileCacheCount = 1000, int threadsCount = 0,
                                                                   IProgress<double> progress = null,
                                                                   bool isPrintEstimatedTime = false)
        {
            Channel<ITile> channel = Channel.CreateUnbounded<ITile>();

            WriteTilesToChannelAsync(channel.Writer, minZ, maxZ, tmsCompatible, tileSize,
                                     interpolation, bandsCount, tileCacheCount,
                                     threadsCount, progress, isPrintEstimatedTime)
               .ContinueWith(_ => channel.Writer.Complete(), TaskScheduler.Current);

            return channel.Reader.ReadAllAsync();
        }

        #endregion

        #endregion
    }
}

#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning restore CA1062 // Check args
