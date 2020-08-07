﻿#pragma warning disable CA1031 // Do not catch general exception types

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GTiff2Tiles.Core.Constants;
using GTiff2Tiles.Core.Coordinates;
using GTiff2Tiles.Core.Enums;
using GTiff2Tiles.Core.Exceptions;
using GTiff2Tiles.Core.Helpers;
using GTiff2Tiles.Core.Images;

// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace GTiff2Tiles.Core.Tiles
{
    /// <summary>
    /// Basic implementation of <see cref="ITile"/> interface
    /// </summary>
    public class Tile : ITile
    {
        #region Properties

        /// <summary>
        /// Default value of <see cref="Tile"/>'s side size
        /// </summary>
        private const int DefaultSideSizeValue = 256;

        /// <summary>
        /// Default <see cref="Tile"/>'s <see cref="Images.Size"/>
        /// </summary>
        public static readonly Size DefaultSize = new Size(DefaultSideSizeValue, DefaultSideSizeValue);

        /// <summary>
        /// <see cref="Tile"/>s with <see cref="Bytes"/> count lesser
        /// than this value won't pass <see cref="Validate(bool,int)"/> check
        /// </summary>
        public const int MinimalBytesCount = 355;

        /// <inheritdoc />
        public bool IsDisposed { get; set; }

        /// <inheritdoc />
        public GeoCoordinate MinCoordinate { get; }

        /// <inheritdoc />
        public GeoCoordinate MaxCoordinate { get; }

        /// <inheritdoc />
        public Number Number { get; }

        /// <inheritdoc />
        public IEnumerable<byte> Bytes { get; set; }

        /// <inheritdoc />
        public Size Size { get; }

        /// <inheritdoc />
        public string Path { get; set; }

        /// <inheritdoc />
        public TileExtension Extension { get; }

        /// <inheritdoc />
        public bool TmsCompatible { get; }

        #endregion

        #region Constructors/Destructors

        /// <summary>
        /// Creates new <see cref="Tile"/>
        /// </summary>
        /// <param name="number"><see cref="Number"/></param>
        /// <param name="coordinateSystem">Desired coordinate system</param>
        /// <param name="size"><see cref="Size"/>;
        /// <remarks>should be a square, e.g. 256x256</remarks></param>
        /// <param name="bytes"><see cref="Bytes"/></param>
        /// <param name="extension"><see cref="Extension"/></param>
        /// <param name="tmsCompatible">Is tms compatible?</param>
        /// <exception cref="TileException"></exception>
        protected Tile(Number number, CoordinateSystem coordinateSystem, Size size = null,
                       IEnumerable<byte> bytes = null, TileExtension extension = TileExtension.Png,
                       bool tmsCompatible = false)
        {
            (Number, Bytes, Extension, TmsCompatible, Size) = (number, bytes, extension, tmsCompatible, size ?? DefaultSize);

            if (!CheckSize()) throw new TileException();

            (MinCoordinate, MaxCoordinate) = Number.ToGeoCoordinates(coordinateSystem, Size, tmsCompatible);
        }

        /// <summary>
        /// Creates new <see cref="Tile"/> from <see cref="GeoCoordinate"/> values
        /// </summary>
        /// <param name="minCoordinate">Minimum <see cref="GeoCoordinate"/></param>
        /// <param name="maxCoordinate">Maximum <see cref="GeoCoordinate"/></param>
        /// <param name="zoom">Zoom</param>
        /// <param name="size"><see cref="Size"/>;
        /// <remarks>should be a square, e.g. 256x256</remarks></param>
        /// <param name="bytes"><see cref="Bytes"/></param>
        /// <param name="extension"><see cref="Extension"/></param>
        /// <param name="tmsCompatible">Is tms compatible?</param>
        /// <exception cref="TileException"></exception>
        protected Tile(GeoCoordinate minCoordinate, GeoCoordinate maxCoordinate, int zoom, Size size = null,
                       IEnumerable<byte> bytes = null, TileExtension extension = TileExtension.Png,
                       bool tmsCompatible = false)
        {
            Size = size ?? DefaultSize;

            if (!CheckSize()) throw new TileException();

            (Number minNumber, Number maxNumber) = GeoCoordinate.GetNumbers(minCoordinate, maxCoordinate, zoom, Size.Width, tmsCompatible);

            if (!minNumber.Equals(maxNumber))
                throw new TileException();

            (Number, Bytes, Extension, TmsCompatible) = (minNumber, bytes, extension, tmsCompatible);
            (MinCoordinate, MaxCoordinate) = (minCoordinate, maxCoordinate);
        }

        /// <summary>
        /// Calls <see cref="Dispose(bool)"/> on this <see cref="Tile"/>.
        /// </summary>
        ~Tile() => Dispose(false);

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
                // Occurs only if called by programmer. Dispose static things here.
            }

            Bytes = null;

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
                return new ValueTask(Task.FromException(exception));
            }
        }

        #endregion

        #region CheckSize

        /// <summary>
        /// Check if <see cref="ITile"/> is a square
        /// </summary>
        /// <returns><see langword="true"/> if it is a square;
        /// <see langword="false"/> otherwise</returns>
        public bool CheckSize() => CheckSize(this);

        /// <inheritdoc cref="CheckSize()"/>
        /// <param name="tile"><see cref="ITile"/> to check</param>
        /// <returns></returns>
        public static bool CheckSize(ITile tile)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile));

            return tile.Size.Width == tile.Size.Height;
        }

        #endregion

        #region Validate

        /// <inheritdoc />
        public bool Validate(bool isCheckPath, int minimalBytesCount = MinimalBytesCount)
            => Validate(this, isCheckPath);

        /// <inheritdoc cref="Validate(bool, int)"/>
        /// <param name="tile"><see cref="Tile"/> to check</param>
        /// <param name="isCheckPath"></param>
        /// <param name="minimalBytesCount"></param>
        public static bool Validate(ITile tile, bool isCheckPath, int minimalBytesCount = MinimalBytesCount)
        {
            if (tile?.Bytes == null || tile.Bytes.Count() <= minimalBytesCount) return false;

            return !isCheckPath || CheckHelper.CheckFile(tile.Path);
        }

        #endregion

        #region CalculatePosition

        /// <inheritdoc />
        public int CalculatePosition() => CalculatePosition(Number, TmsCompatible);

        /// <inheritdoc cref="CalculatePosition()"/>
        /// <param name="number"><see cref="Tiles.Number"/> of <see cref="Tile"/></param>
        /// <param name="tmsCompatible">Is tms compatible?</param>
        public static int CalculatePosition(Number number, bool tmsCompatible)
        {
            // 0 1
            // 2 3

            if (number == null) throw new ArgumentNullException(nameof(number));

            int tilePosition;

            if (tmsCompatible)
            {
                if (number.X % 2 == 0) tilePosition = number.Y % 2 == 0 ? 2 : 0;
                else tilePosition = number.Y % 2 == 0 ? 3 : 1;
            }
            else
            {
                if (number.X % 2 == 0) tilePosition = number.Y % 2 == 0 ? 0 : 2;
                else tilePosition = number.Y % 2 == 0 ? 1 : 3;
            }

            return tilePosition;
        }

        #endregion

        #region GetExtensionString

        /// <inheritdoc />
        public string GetExtensionString() => GetExtensionString(Extension);

        /// <param name="extension"><see cref="TileExtension"/> to convert</param>
        /// <inheritdoc cref="GetExtensionString()"/>
        public static string GetExtensionString(TileExtension extension) => extension switch
        {
            TileExtension.Png => FileExtensions.Png,
            TileExtension.Jpg => FileExtensions.Jpg,
            TileExtension.Webp => FileExtensions.Webp,
            _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
        };

        #endregion

        #endregion
    }
}

#pragma warning restore CA1031 // Do not catch general exception types
