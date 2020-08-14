﻿#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable CS0219 // The variable is assigned but it's value is never used
#pragma warning disable CA1031 // Do not catch general exception types

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GTiff2Tiles.Core.Coordinates;
using GTiff2Tiles.Core.Enums;
using GTiff2Tiles.Core.Images;
using GTiff2Tiles.Core.Tiles;
using GTiff2Tiles.Tests.Constants;
using NUnit.Framework;

// ReSharper disable UnusedVariable

namespace GTiff2Tiles.Tests.Tests.Tiles
{
    [TestFixture]
    public sealed class TileTests
    {
        #region SetUp and consts

        private string _timestamp;

        private readonly GeodeticCoordinate _tokyoGeodeticCoordinate = new GeodeticCoordinate(139.839478, 35.652832);

        private readonly Number _tokyoGeodeticNumber = new Number(1819, 309, 10);

        private const CoordinateSystem Cs4326 = CoordinateSystem.Epsg4326;

        [SetUp]
        public void SetUp()
        {
            _timestamp = DateTime.Now.ToString(Core.Constants.DateTimePatterns.LongWithMs,
                                               CultureInfo.InvariantCulture);
            FileSystemEntries.OutputDirectoryInfo.Create();
        }

        #endregion

        #region Constructors

        #region FromNumber

        [Test]
        public void FromNumberDefaultArgs() => Assert.DoesNotThrowAsync(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);
        });

        [Test]
        public void FromNumberOverrideDefaultArgs()
        {
            Size size = new Size(128, 128);
            IEnumerable<byte> bytes = new byte[10];
            const TileExtension extension = TileExtension.Webp;
            const bool tmsCompatible = true;
            const int bandsCount = 3;
            const Interpolation interpolation = Interpolation.Cubic;

            Assert.DoesNotThrowAsync(async () =>
            {
                await using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326, size, bytes, extension, tmsCompatible,
                                                        bandsCount, interpolation);
            });
        }

        [Test]
        public void FromNumberSmallBands() => Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326, bandsCount: -1);
        });

        [Test]
        public void FromNumberMuchBands() => Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326, bandsCount: 5);
        });

        [Test]
        public void FromNumberNotSquare() => Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326, new Size(1, 256));
        });

        #endregion

        #region FromCoordinates

        [Test]
        public void FromCoordinatesDefaultArgs() => Assert.DoesNotThrowAsync(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticCoordinate, _tokyoGeodeticCoordinate, 10);
        });

        [Test]
        public void FromCoordinatesOverrideDefaultArgs()
        {
            Size size = new Size(64, 64);
            IEnumerable<byte> bytes = new byte[10];
            const TileExtension extension = TileExtension.Webp;
            const bool tmsCompatible = true;
            const int bandsCount = 3;
            const Interpolation interpolation = Interpolation.Cubic;

            Assert.DoesNotThrowAsync(async () =>
            {
                await using ITile tile = new RasterTile(_tokyoGeodeticCoordinate, _tokyoGeodeticCoordinate,
                                                        10, size, bytes, extension, tmsCompatible,
                                                        bandsCount, interpolation);
            });
        }

        [Test]
        public void FromCoordinatesSmallBands() => Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticCoordinate, _tokyoGeodeticCoordinate,
                                                    10, bandsCount: -1);
        });

        [Test]
        public void FromCoordinatesMuchBands() => Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticCoordinate, _tokyoGeodeticCoordinate,
                                                    10, bandsCount: 5);
        });

        [Test]
        public void FromCoordinatesNotSquare() => Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticCoordinate, _tokyoGeodeticCoordinate,
                                                    10, new Size(1, 256));
        });

        [Test]
        public void FromCoordinatesMinNotEqualsMax()
        {
            GeoCoordinate min = new GeodeticCoordinate(0.0, 0.0);
            GeoCoordinate max = new GeodeticCoordinate(180.0, 90.0);
            const int zoom = 10;

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await using ITile tile = new RasterTile(min, max, zoom);
            });
        }

        #endregion

        #endregion

        #region Destructor/Dispose

        [Test]
        public void DisposeTest()
        {
            ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            Assert.DoesNotThrow(() => tile.Dispose());
            Assert.True(tile.IsDisposed);
        }

        [Test]
        public void DisposeAsyncTest()
        {
            ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            Assert.DoesNotThrowAsync(async () => await tile.DisposeAsync().ConfigureAwait(false));
            Assert.True(tile.IsDisposed);
        }

        #endregion

        #region Properties/Constants

        [Test]
        public void GetConstants() => Assert.DoesNotThrow(() =>
        {
            Size size = Tile.DefaultSize;
            int bandsCount = RasterTile.DefaultBandsCount;
        });

        [Test]
        public void GetProperties()
        {
            ITile tile = null;
            Assert.DoesNotThrow(() =>
            {
                tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

                bool isDisposed = tile.IsDisposed;
                GeoCoordinate minC = tile.MinCoordinate;
                GeoCoordinate maxC = tile.MaxCoordinate;
                Number num = tile.Number;
                IEnumerable<byte> bytes = tile.Bytes;
                Size size = tile.Size;
                string path = tile.Path;
                TileExtension ext = tile.Extension;
                int minBytesC = tile.MinimalBytesCount;
            });

            Assert.DoesNotThrowAsync(async () =>
            {
                await using RasterTile rasterTile = (RasterTile)tile;
                int bandsCount = rasterTile.BandsCount;
                Interpolation interpolation = rasterTile.Interpolation;
            });
        }

        [Test]
        public void SetProperties() => Assert.DoesNotThrowAsync(async () =>
        {
            await using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326)
            {
                Bytes = null, Path = string.Empty, MinimalBytesCount = int.MinValue
            };
        });

        #endregion

        #region Methods

        #region Validate

        [Test]
        public void ValidateWoPath()
        {
            using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            tile.Bytes = new byte[tile.MinimalBytesCount + 1];

            Assert.True(tile.Validate(false));
        }

        [Test]
        public void ValidateWPath()
        {
            string tilePath = Path.Combine(FileSystemEntries.OutputDirectoryPath, $"{_timestamp}_validate.png");

            using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            FileStream fs = File.Create(tilePath);
            fs.Dispose();

            tile.Bytes = new byte[tile.MinimalBytesCount + 1];
            tile.Path = tilePath;

            Assert.True(tile.Validate(true));

            File.Delete(tilePath);
        }

        [Test]
        public void ValidateNullTile() => Assert.False(Tile.Validate(null, false));

        [Test]
        public void ValidateNullBytes()
        {
            using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            Assert.False(tile.Validate(false));
        }

        [Test]
        public void ValidateSmallBytes()
        {
            using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            tile.Bytes = new byte[tile.MinimalBytesCount - 1];

            Assert.False(tile.Validate(false));
        }

        [Test]
        public void ValidateNullPath()
        {
            using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            tile.Bytes = new byte[tile.MinimalBytesCount + 1];

            Assert.False(tile.Validate(true));
        }

        #endregion

        #region CalculatePosition

        [Test]
        public void CalculatePosition()
        {
            Number number = _tokyoGeodeticNumber;

            ITile tile = new RasterTile(number, Cs4326);
            int pos3 = tile.CalculatePosition();

            tile = new RasterTile(new Number(number.X + 1, number.Y, number.Z), Cs4326);
            int pos2 = tile.CalculatePosition();

            tile = new RasterTile(new Number(number.X, number.Y + 1, number.Z), Cs4326);
            int pos1 = tile.CalculatePosition();

            tile = new RasterTile(new Number(number.X + 1, number.Y + 1, number.Z), Cs4326);
            int pos0 = tile.CalculatePosition();

            Assert.True(pos0 == 0 && pos1 == 1 && pos2 == 2 && pos3 == 3);

            tile.Dispose();
        }

        [Test]
        public void CalculatePositionNullNumber() => Assert.Throws<ArgumentNullException>(() =>
              Tile.CalculatePosition(null, false));

        #endregion

        #region GetExtensionString

        [Test]
        public void GetExtensionString()
        {
            using ITile tile = new RasterTile(_tokyoGeodeticNumber, Cs4326);

            tile.GetExtensionString();

            Assert.True(Tile.GetExtensionString(TileExtension.Png) == Core.Constants.FileExtensions.Png);
            Assert.True(Tile.GetExtensionString(TileExtension.Jpg) == Core.Constants.FileExtensions.Jpg);
            Assert.True(Tile.GetExtensionString(TileExtension.Webp) == Core.Constants.FileExtensions.Webp);
        }

        #endregion

        #endregion
    }
}

#pragma warning restore IDE0059 // Unnecessary assignment of a value
#pragma warning restore CS0219 // The variable is assigned but it's value is never used
#pragma warning restore CA1031 // Do not catch general exception types
