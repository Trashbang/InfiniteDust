using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteDust
{
    // Some notes on the scale, properties and proportions of DE_DUST2. Probably will be necessary at some point.

    // Playable space bounding box: 3840x4128 (okay so, let's say 4096x4096?)

    // Max ledge height (excluding overpass): 160

    // Metal crates are primarily found around sites, but there may also be a few in mid
    // Wooden crates are usually pressed up against walls. They range in size from 48x48 to 144x144.
    // Larger crates are mostly found as part of stacks of smaller crates, but small crates often exist on their own

    // Floor types include: road, sand, stone. Road and sand can be mixed freely, but stone may only be connected
    // to road or sand via steps (typically one or two)
    // Most paths are defined by the cobbled roads, which ALWAYS terminate in closed double-doors.

    // The double-door arch is a prefab, which may or may not contain doors. However, custom arches also exist throughout the map,
    // serving primarily as transitions between underground and overground spaces, or between corridors and chambers
    // Arches typically begin at 64 units above ground level, but may go up to 80. A special arch door prefab also exists in some walls,
    // purely as detail. It is usually placed 16 units above ground and given a step which juts out.

    // Ceilings are usually completely flat for the entirely of the underground space, unless two underground spaces are joined.

    // Underground spaces are lit with fluorescent bar light prefabs, placed on the wall. Their placement varies,
    // but it appears to favour the (horizontal) middle of the face, and can be anywhere from 'close to the ceiling'
    // to 'nearly eye-level'

    // Protruding corners are sometimes bevelled at 64 units, or (rarely) at 16
    // They may also be marked by towers, which protrude 32 units out from the wall, are slightly taller than the wall, and measure 128x128 to 192x192.

    // If one flat area is higher than another flat area, it will typically have a low trimmed wall of 16x32. In rare cases it may also be 16x16 or 32x32.

    // Walls *can* be perfectly straight for up to 1600 units, but tend to go 300-600 before being broken up by details or changing course.
    // If they reach their full height in the open air, they tend to have a 16x32 bevel on top, but may also recede in a 1:4 slope
    // after reaching a certain height (typically a safe 160 units above ground)

    // Sand may pile up in right-angle corners, producing slightly raised terrain. It may also pile up against a wall in a 4:1 slope.

    // Try to remember Hammer uses Z-up, won't you?
    public struct Coords
    {
        public int x;
        public int y;
        public int z;

        public Coords(Coords copy)
        {
            this.x = copy.x;
            this.y = copy.y;
            this.z = copy.z;
        }

        public Coords(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Coords operator +(Coords a, Vector b)
        {
            return new Coords((int)Math.Round(a.x + b.x), (int)Math.Round(a.y + b.y), (int)Math.Round(a.z + b.z));
        }

        public string ToMapString() // As opposed to ToString, this supplies a properly formatted representation for writing to .map
        {
            return "( " + x + " " + y + " " + z + " )"; // I don't know how particular the tools are about the spacing here, but I don't intend to find out
        }
    }

    // Hello welcome to maths hell
    public struct Vector
    {
        public const int MAX_TRACE_LENGTH = 8192;
        public double x;
        public double y;
        public double z;

        public Vector(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector(Vector vec)
        {
            this.x = vec.x;
            this.y = vec.y;
            this.z = vec.z;
        }

        // Vector from A to B
        public Vector(Coords a, Coords b)
        {
            this.x = b.x - a.x;
            this.y = b.y - a.y;
            this.z = b.z - a.z;
        }

        public Vector(Vector a, Vector b)
        {
            this.x = b.x - a.x;
            this.y = b.y - a.y;
            this.z = b.z - a.z;
        }

        public static Vector Up()
        {
            return new Vector(0, 0, 1);
        }

        // Cross product of two other vectors
        public static Vector CrossProduct(Vector a, Vector b)
        {
            return new Vector((a.y * b.z) - (a.z * b.y), (a.z * b.x) - (a.x * b.z), (a.x * b.y) - (a.y * b.x));
        }


        // Makes a unit vector, necessary for some calculations
        public static Vector Normalise(Vector vec)
        {
            double magnitude = Magnitude(vec);
            double x = vec.x / magnitude;
            double y = vec.y / magnitude;
            double z = vec.z / magnitude;
            return new Vector(x, y, z);
        }

        // Returns the product of rotating a given vector around an arbitrary axis in space.
        public static Vector RotateAboutAxis(Vector vector, Vector axis, double angle)
        {
            axis = Vector.Normalise(axis);
            double x = vector.x * (Math.Cos(angle) + axis.x * axis.x * (1 - Math.Cos(angle))) + vector.y * (axis.x * axis.y * (1 - Math.Cos(angle)) - axis.z * Math.Sin(angle)) + vector.z * (axis.x * axis.z * (1 - Math.Cos(angle)) + axis.y * Math.Sin(angle));
            double y = vector.x * (axis.y * axis.x * (1 - Math.Cos(angle)) + axis.z * Math.Sin(angle)) + vector.y * (Math.Cos(angle) + axis.y * axis.y * (1 - Math.Cos(angle))) + vector.z * (axis.y * axis.z * (1 - Math.Cos(angle)) - axis.x * Math.Sin(angle));
            double z = vector.x * (axis.z * axis.x * (1 - Math.Cos(angle)) - axis.y * Math.Sin(angle)) + vector.y * (axis.z * axis.y * (1 - Math.Cos(angle)) + axis.x * Math.Sin(angle)) + vector.z * (Math.Cos(angle) + axis.z * axis.z * (1 - Math.Cos(angle)));
            return new Vector(x, y, z);
        }
        
        public static double Magnitude(Vector vec)
        {
            return Math.Sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
        }

        public static double DotProduct(Vector a, Vector b)
        {
            return (a.x * b.x + a.y * b.y + a.z * b.z);
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        
        public static Vector operator *(Vector a, int b)
        {
            return new Vector(a.x * b, a.y * b, a.z * b);
        }

        public static bool Equals(Vector a, Vector b)
        {
            bool isEqual = false;
            if ((a.x == b.x) && (a.y == b.y) && (a.z == b.z))
            {
                isEqual = true;
            }
            return isEqual;
        }

        public static bool BasicallyEquals(Vector a, Vector b, double margin)
        {
            bool isEqual = false;
            if ((Math.Abs(a.x - b.x) < margin) && (Math.Abs(a.y - b.y) < margin) && (Math.Abs(a.z - b.z) < margin))
            {
                isEqual = true;
            }
            return isEqual;
        }

        public bool Equals(Vector a)
        {
            return Equals(this, a);
        }

        public bool BasicallyEquals(Vector a, double margin)
        {
            return BasicallyEquals(this, a, margin);
        }

        // Uses degrees
        public static double AngleBetween(Vector a, Vector b)
        {
            return (Math.Acos(DotProduct(a, b) / (Magnitude(a) * Magnitude(b)))) * 180 / Math.PI;
        }

        // Traces from (origin) in (direction) until it hits a member of (brushes)
        public static TraceInfo Trace(Coords origin, Vector direction, IEnumerable<MapFile.Brush> brushes, int maxLength)
        {
            TraceInfo results = new TraceInfo(origin);
            Vector dir = Vector.Normalise(direction);
            Coords location = new Coords(origin);
            int traceLen = 0;
            while (!results.hit && (traceLen <= maxLength))
            {
                location = new Coords((int)Math.Round(origin.x + dir.x * traceLen), (int)Math.Round(origin.y + dir.y * traceLen), (int)Math.Round(origin.z + dir.z * traceLen)); // this is a little gross, maybe it could be turned into a constructor?
                foreach (MapFile.Brush brush in brushes)
                {
                    // How do we test for collision with a brush, anyway?
                    // A trace has 'entered' a brush when it is on the reverse side of ALL the planes in it
                    // The dot product of two vectors is positive if the angle between them is 0 -> 90 or 270 -> 360 and negative if it's 90 -> 270. At exactly 90 or 270, it's 0.
                    // Therefore, if the dot product of the plane normal and a vector BETWEEN the trace position and the plane is <=0, it's on the far side
                    bool isInside = true;
                    foreach (Plane plane in brush.planes)
                    {
                        Coords planePoint = plane.p1; // just an arbitrary point on the plane
                        Vector outwardNormal = plane.Normal();
                        Vector traceOffset = new Vector(location.x - planePoint.x, location.y - planePoint.y, location.z - planePoint.z);
                        if (Vector.DotProduct(outwardNormal, traceOffset) >= 0) // Angle between them is <90 degrees, so the point is on the outside of at least one plane
                        {
                            isInside = false;
                            break;
                        }
                    }
                    if (isInside)
                    {
                        results.hit = true;
                        results.impactBrush = brush;
                        break;
                    }
                }
                // Messy, but we have to backtrack one step so our trace finishes ON a plane (and not inside the brush)
                if (results.hit)
                {
                    results.finishPoint = new Coords((int)Math.Round(location.x - dir.x), (int)Math.Round(location.y - dir.y), (int)Math.Round(location.z - dir.z));
                    results.traceLen = traceLen - 1;
                    if (results.traceLen < 0)
                    {
                        results.traceLen = 0;
                    }
                }
                else
                {
                    results.finishPoint = location;
                    results.traceLen = traceLen;
                }

                // move forward a smidgen
                traceLen++;
            }
            return results;
        }

        // Alternate version of trace to determine when we reach the OUTSIDE of a brush.
        public static TraceInfo InteriorTrace(Coords origin, Vector direction, MapFile.Brush brush, int maxLength)
        {
            TraceInfo results = new TraceInfo(origin);
            Vector dir = Vector.Normalise(direction);
            Coords location = new Coords(origin);
            int traceLen = 0;
            while (!results.hit && (traceLen < maxLength))
            {
                location = new Coords((int)Math.Round(origin.x + dir.x * traceLen), (int)Math.Round(origin.y + dir.y * traceLen), (int)Math.Round(origin.z + dir.z * traceLen));

                // How do we test when we are outside a brush?
                // It's more or less the inverse of above: a trace has exited a brush when it is on the outside of AT LEAST ONE plane
                // The dot product of two vectors is positive if the angle between them is 0 -> 90 or 270 -> 360 and negative if it's 90 -> 270. At exactly 90 or 270, it's 0.
                // Therefore, if the dot product of the plane normal and a vector BETWEEN the trace position and the plane is >0, it's outside
                bool isInside = true;
                foreach (Plane plane in brush.planes)
                {
                    Coords planePoint = plane.p1; // just an arbitrary point on the plane
                    Vector outwardNormal = plane.Normal();
                    Vector traceOffset = new Vector(location.x - planePoint.x, location.y - planePoint.y, location.z - planePoint.z);
                    if (Vector.DotProduct(outwardNormal, traceOffset) >= 0) // Angle between them is <90 degrees, so the point is on the outside of at least one plane
                    {
                        isInside = false;
                        results.impactPlane = plane;
                        break;
                    }
                }
                if (!isInside)
                {
                    results.hit = true;
                }
                results.finishPoint = location;
                results.traceLen = traceLen;
                traceLen++;

            }
            return results;
        }

        public struct TraceInfo
        {
            public bool hit; // Did we actually hit something, or did our trace expire?
            public double traceLen; // How far did we travel?
            public Coords startPoint; // Where did we start from?
            public Coords finishPoint; // Where did we finish?
            public MapFile.Brush impactBrush; // What did we hit? (can be null, if trace expires)
            public Plane impactPlane;
            
            public TraceInfo(Coords origin)
            {
                hit = false;
                traceLen = 0;
                startPoint = new Coords(origin);
                finishPoint = new Coords(origin);
                impactBrush = null;
                impactPlane = null;
            }
        }



        public string ToMapString()
        {
            return this.x + " " + this.y + " " + this.z;
        }

    }

    public class Plane
    {
        // Each plane is defined by three non-colinear points (winding clockwise facing out).
        // We could use arbitrary points, but it's smarter to just use three vertices
        // from the face we want to define
        public Coords p1;
        public Coords p2;
        public Coords p3;

        // texRight and texDown define the plane that the texture is projected from
        // while xOffset and yOffset define the distance that the texture is shifted
        // in those vectors' direction
        public string texName;
        public Vector texRight;
        public Vector texDown;
        public int xOffset;
        public int yOffset;
        public int rotation;
        public double scaleX;
        public double scaleY;

        // I know, I know
        public Plane(Coords p1, Coords p2, Coords p3, string texName, Vector texRight, Vector texDown, int xOffset, int yOffset, int rotation, double scaleX, double scaleY)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;

            this.texName = texName;
            this.texRight = texRight;
            this.texDown = texDown;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.rotation = rotation;
            this.scaleX = scaleX;
            this.scaleY = scaleY;
        }

        // Default rotation, scaling, offsets
        public Plane(Coords p1, Coords p2, Coords p3, string texName, Vector texRight, Vector texDown) : this(p1, p2, p3, texName, texRight, texDown, 0, 0, 0, 1.0, 1.0) { }

        // Default rotation, scaling, offsets, alignment.
        // Textures are either aligned to face (a direct product of the face's vertices) or to world (nearest cartesian alignment that's less than perpendicular)
        // Christ, help me
        public Plane(Coords p1, Coords p2, Coords p3, string texName, bool alignToWorld)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;

            this.texName = texName;

            this.xOffset = 0;
            this.yOffset = 0;
            this.rotation = 0;
            this.scaleX = 1.0;
            this.scaleY = 1.0;

            // A and B are the vectors that define the plane (we'll need them in a sec)
            Vector a = new Vector(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
            Vector b = new Vector(p3.x - p2.x, p3.y - p2.y, p3.z - p2.z);
            Vector normal = Vector.CrossProduct(a, b);

            // If we're already aligned to a coordinate plane (X-Y, Y-Z, X-Z) then face alignment is the same as world alignment
            // So let's just do that, mmm?
            // (Coordinate planes have a normal with two components = 0)
            if (alignToWorld || (normal.x == 0 && normal.y == 0) || (normal.y == 0 && normal.z == 0) || (normal.x == 0 && normal.z == 0))
            {
                // 'Align to world' is just shorthand for 'align to nearest cartesian plane'.
                // There are only three of these planes (well, six, because we need to distinguish between each side)
                // and our alignment will fit best with the plane whose normal closest matches ours.
                // So measure the angle between them and choose the one with the smallest degree of separation.
                int smallestAngle = 360;
                int nextAngle;
                Vector texNormal = new Vector(1, 1, 1);

                Vector[] cartVectors = { new Vector(0, 0, 1), new Vector(0, 0, -1), new Vector(0, 1, 0), new Vector(0, -1, 0), new Vector(1, 0, 0), new Vector(-1, 0, 0) };
                foreach (Vector cartVector in cartVectors)
                {
                    nextAngle = (int)(Vector.AngleBetween(normal, cartVector));
                    if (nextAngle < smallestAngle)
                    {
                        smallestAngle = nextAngle;
                        texNormal = cartVector;
                    }
                }

                // For X-Z and Y-Z aligned normals, the down vector is -Z. For X-Y-aligned normals, the down vector is -Y
                if (texNormal.x == 0 && texNormal.y == 0)
                {
                    this.texDown = new Vector(0, -1, 0);
                }
                else
                {
                    this.texDown = new Vector(0, 0, -1);
                }
                this.texRight = Vector.CrossProduct(texNormal, this.texDown);
            }
            else
            {
                // Finally... a chance to use my degree
                // Rotate around the surface of the plane and find the best candidate for 'down'
                // (i.e. the vector with the most negative Z component)
                Vector mostDownwardVector = a;
                for (int i = 0; i < 360; i++)
                {
                    Vector candidate = Vector.RotateAboutAxis(a, normal, (double)(i) * Math.PI / 180);
                    if (candidate.z < mostDownwardVector.z)
                    {
                        mostDownwardVector = candidate;
                    }
                }
                this.texDown = mostDownwardVector;
                this.texRight = Vector.CrossProduct(normal, this.texDown);
            }
            // The magnitude shouldn't matter, but I'm normalising them to be sure
            this.texDown = Vector.Normalise(this.texDown);
            this.texRight = Vector.Normalise(this.texRight);
        }

        // Get the OUTWARD-FACING normal of the plane
        public Vector Normal()
        {
            Vector a = new Vector(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
            Vector b = new Vector(p3.x - p2.x, p3.y - p2.y, p3.z - p2.z);
            return Vector.Normalise(Vector.CrossProduct(a, b));
        }

        // Get inward-facing normal
        public Vector InverseNormal()
        {
            Vector normal = this.Normal();
            Vector invNormal = new Vector(normal.x * -1, normal.y * -1, normal.z * -1);
            return invNormal;
        }

        // It's necessary to work with planes in the form 'ax + by + cz + d = 0' for certain calculations
        // Getting a, b and c is trivial (they're just the normal components),
        // but getting d from the three-point form is not. This function returns that component.
        public double getD()
        {
            // If we know a point on the plane, we can substitute it as the x, y, z in the above form
            Coords p = this.p1;
            Vector n = this.Normal();
            return -(n.x * p.x + n.y * p.y + n.z * p.z);
        }

        // Return the edge that results from two planes intersecting, in the form of a point and a direction.
        // Note that the planes MAY NOT actually intersect (if they're parallel, for instance).
        // In this case, it will return the nonsense value (0, 0, 0) (0, 0, 0)
        public static (Vector vec, Coords point) Intersection(Plane p1, Plane p2)
        {
            Vector n1 = p1.Normal();
            Vector n2 = p2.Normal();
            double d1 = p1.getD();
            double d2 = p2.getD();
            Vector cross = Vector.CrossProduct(n1, n2);
            Vector vec = new Vector(0, 0, 0);
            Coords point = new Coords(0, 0, 0);

            // If the cross-product is a zero vector, the planes are parallel, and going further is futile
            if (!cross.Equals(new Vector(0, 0, 0)))
            {
                // We have the cross product - a vector that describes the line of intersection of the two planes.
                // Now we need a point that satisfies both planes, which means solving simultaneous equations.
                // We can set one component, so let's make z = 0
                // Now we solve with Cramer's Rule.
                // This normally uses matrices and determinants, but it's kind of a waste when you can't see the fancy notation
                double cDet = n1.x * n2.y - n1.y * n2.x;
                double xDet = -d1 * n2.y - n1.y * -d2;
                double yDet = -d2 * n1.x - n2.x * -d1;

                // With any luck, the results are *pretty close* to integer values.
                // Nevertheless, because of how .maps work, we're gonna have to round the coordinates to nearest
                vec = new Vector(cross);
                point.x = (int)(Math.Round(xDet / cDet));
                point.y = (int)(Math.Round(yDet / cDet));
                point.z = 0;
            }

            return (vec, point);
        }
    }

    // Defines what an abstract Location is 'for'.
    // Note that this doesn't define the bounds of these areas.
    // Also 'none' doesn't mean that the location is literally pointless, just that it doesn't have any functionality related to the game's rules.
    // It's just a space, y'know?
    // INACCESSIBLE means there aren't (or shouldn't be, at least) any paths connected to it, so it should be ignored when building the actual layout
    public enum LocationPurpose { NONE, CT_SPAWN, T_SPAWN, SITE_A, SITE_B, INACCESSIBLE };

    // Abstract representation of a 'place' within the map; a meeting of routes, an arena, a space.
    // The location of this Location (ha ha) isn't particularly crucial; we just have to define it for the sake of the high-level structure
    // This is a stupid class, I know, but we're using it instead of a struct because we need to use references
    public class Location
    {
        public Coords coords;
        public LocationPurpose purpose;

        public Location()
        {
            purpose = LocationPurpose.NONE;
        }
    }

    // Joins two Locations. Represents a route; a means of getting from one area to the next. Once again, this is purely abstract,
    // and may not ultimately manifest as a genuinely recognisable corridor or road
    public struct Path
    {
        private Location[] locations;

        // To hopefully minimise the likelihood of dangling paths, you can only retrieve or set Locations in pairs
        public Location[] GetLocations()
        {
            return locations;
        }
        public void SetLocations(Location l1, Location l2)
        {
            locations[0] = l1;
            locations[1] = l2;
        }
        public Path(Location l1, Location l2) // Creation and assignment in one go, for convenience
        {
            locations = new Location[2];
            locations[0] = l1;
            locations[1] = l2;
        }
    }

    // High-level representation of the map as a series of routes and locations. Doesn't handle brushwork, just organisation
    public class AbstractLayout
    {

        public const int MAX_VERTICAL_VARIANCE = 0; // Maximum height difference between 'connected' Locations. Tweak as necessary.
        public const int MIN_VERTICAL_VARIANCE = 0;
        public const int BASE_HORIZONTAL_SEPARATION = 2048; // Standard (average?) distance between any two locations. Derived from Dust2 being approx 4096x4096
        public const int MAX_HORIZONTAL_VARIANCE = 512; // Max distance a location can deviate from the average
        public const int MIN_HORIZONTAL_VARIANCE = 0; // Probably will never need this but what the hell

        public Location[,] locales;
        public List<Path> paths;

        // Constructs a basic abstract AbstractLayout with no spatial information
        // (i.e. it's just the nodes, and which links exist between them)
        //
        //    B---CT---A
        //    |   |    |
        //    +---+----+
        //    |   |    |
        //    +---T----+

        public AbstractLayout()
        {
            locales = new Location[3, 3];
            for (int i = 0; i < 3; i++) { for (int j = 0; j < 3; j++)
            {
                locales[i, j] = new Location();
            }}
            paths = new List<Path>();

            // Basic four-square structure. Might mess with this later.
            locales[0, 0].purpose = LocationPurpose.SITE_B;
            locales[1, 0].purpose = LocationPurpose.CT_SPAWN;
            locales[2, 0].purpose = LocationPurpose.SITE_A;

            locales[0, 1].purpose = LocationPurpose.NONE;
            locales[1, 1].purpose = LocationPurpose.NONE;
            locales[2, 1].purpose = LocationPurpose.NONE;

            locales[0, 2].purpose = LocationPurpose.NONE;
            locales[1, 2].purpose = LocationPurpose.T_SPAWN;
            locales[2, 2].purpose = LocationPurpose.NONE;

            // Twelve Paths, one between each pair of adjacent Locations (no diagonals)
            paths.Add(new Path(locales[0, 0], locales[1, 0]));
            /*paths.Add(new Path(locales[1, 0], locales[2, 0]));
            paths.Add(new Path(locales[0, 1], locales[1, 1]));
            paths.Add(new Path(locales[1, 1], locales[2, 1]));
            paths.Add(new Path(locales[0, 2], locales[1, 2]));
            paths.Add(new Path(locales[1, 2], locales[2, 2]));

            paths.Add(new Path(locales[0, 0], locales[0, 1]));
            paths.Add(new Path(locales[0, 1], locales[0, 2]));
            paths.Add(new Path(locales[1, 0], locales[1, 1]));
            paths.Add(new Path(locales[1, 1], locales[1, 2]));
            paths.Add(new Path(locales[2, 0], locales[2, 1]));
            paths.Add(new Path(locales[2, 1], locales[2, 2]));*/
        }

        // 'allowVariation' lets the constructor deviate somewhat from the standard four-square AbstractLayout
        // if it's false, this just acts like the default constructor
        public AbstractLayout(bool allowVariation) : this()
        {
            double rngThreshold = 0.75; // tweak this if you gotta, it's pretty arbitrary
            Random rng = new Random();
            if (allowVariation)
            {
                // Here, we make use of the knowledge that the paths created in the default constructor are always left-to-right, top-to-bottom.
                // This makes the following mess slightly more concise, but be wary of making this assumption elsewhere

                // First permutation step: potentially remove bottom-left and bottom-right locales and relocate paths to match
                if (rng.NextDouble() > rngThreshold)
                {
                    paths.RemoveAll(path => path.GetLocations()[0].Equals(locales[2, 1]) && path.GetLocations()[1].Equals(locales[2, 2]));
                    int index = paths.FindIndex(path => path.GetLocations()[0].Equals(locales[1, 2]) && path.GetLocations()[1].Equals(locales[2, 2]));
                    paths[index].SetLocations(locales[1, 2], locales[2, 1]);
                    locales[2, 2].purpose = LocationPurpose.INACCESSIBLE;
                }
                if (rng.NextDouble() > rngThreshold)
                {
                    paths.RemoveAll(path => path.GetLocations()[0].Equals(locales[0, 1]) && path.GetLocations()[1].Equals(locales[0, 2]));
                    int index = paths.FindIndex(path => path.GetLocations()[0].Equals(locales[0, 2]) && path.GetLocations()[1].Equals(locales[1, 2]));
                    paths[index].SetLocations(locales[0, 1], locales[1, 2]);
                    locales[0, 2].purpose = LocationPurpose.INACCESSIBLE;
                }
                // Second permutation step: Potentially remove lateral paths from Mid to middle-left/middle-right locations
                if (rng.NextDouble() > rngThreshold)
                {
                    paths.RemoveAll(path => path.GetLocations()[0].Equals(locales[1, 1]) && path.GetLocations()[1].Equals(locales[2, 1]));
                }
                if (rng.NextDouble() > rngThreshold)
                {
                    paths.RemoveAll(path => path.GetLocations()[0].Equals(locales[0, 1]) && path.GetLocations()[1].Equals(locales[1, 1]));
                }
                // Third permutation step: Potentially add paths from mid directly to sites
                if (rng.NextDouble() > rngThreshold)
                {
                    paths.Add(new Path(locales[1, 1], locales[2, 0]));
                }
                if (rng.NextDouble() > rngThreshold)
                {
                    paths.Add(new Path(locales[1, 1], locales[0, 0]));
                }
            }
        }

        // Creates coordinate sets for each Location so they have spatial placement and distance.
        // This might be better off as part of the constructor, but I'm keeping them separate for now
        // (in case this becomes debugging hell)
        public void GenerateSpatialCoords(int minVvariance, int maxVvariance, int avgHseparation, int maxHvariance, int minHvariance)
        {
            Random rng = new Random();
            locales[1, 1].coords = new Coords(0, 0, 0); // I'm pretty sure I don't even need this line but I'm leaving it in, in case I change the generation code

            // There are probably a ton of ways to do this shit, don't be afraid to toy with this code
            for (int i = 0; i <= 2; i++)
            {
                for (int j = 0; j <= 2; j++)
                {
                    // reducing our indices by 1 here because the 'centre' is at index [1, 1] and we want the multiplication to be easy
                    int hOffset = rng.Next(minHvariance, maxHvariance);
                    if (rng.NextDouble() > 0.5) { hOffset *= -1; }
                    locales[i, j].coords.x = (i - 1) * avgHseparation + hOffset;

                    hOffset = rng.Next(minHvariance, maxHvariance);
                    if (rng.NextDouble() > 0.5) { hOffset *= -1; }
                    locales[i, j].coords.y = (j - 1) * -avgHseparation + hOffset;

                    int vOffset = rng.Next(minVvariance, maxVvariance);
                    if (rng.NextDouble() > 0.5) { vOffset *= -1; }
                    locales[i, j].coords.z = vOffset;
                }
            }

        }

        // Default override that uses the built-in const values (use this, preferably)
        public void GenerateSpatialCoords()
        {
            this.GenerateSpatialCoords(MIN_VERTICAL_VARIANCE, MAX_VERTICAL_VARIANCE, BASE_HORIZONTAL_SEPARATION, MAX_HORIZONTAL_VARIANCE, MIN_HORIZONTAL_VARIANCE);
        }
    }

    // Handles logging of events, errors, etc.
    public class Logger
    {
        public static void log(string message)
        {
            // hey uh, remember to implement this at some point, yeah?
        }
    }

    // Handle for interfacing with .map files. The format is reasonably sensible but it's still best that we have some level of abstraction.
    // Also, you know, IO stuff
    public class MapFile
    {
        // Runtime representation of a map entity. Because it's easier to handle these things *before* they get written to file
        // There are two types of entity: Point and Brush. 
        // The main difference between them is that point entities have an origin, while brush entities have a list of brushes
        // In both cases, the entity consists largely of a list of properties, corresponding to Hammer keyvalues.
        // The 'classname' property is critical, since it defines the class of the entity. Therefore, we force the default constructor to take it
        public abstract class MapEnt
        {
            public Dictionary<string, string> properties;

            public MapEnt(string className)
            {
                properties = new Dictionary<string, string>();
                properties.Add("classname", className);
            }
            public string ToMapString() // As opposed to ToString, this supplies a properly formatted representation of the entity that can be written to the .map file
            {
                string asString = "{\r\n";
                foreach (KeyValuePair<string, string> entry in properties)
                {
                    asString += "\t\"" + entry.Key + "\" \"" + entry.Value + "\"\r\n";
                }
                asString += "}\r\n";
                return asString;
            }
        }

        public class PointEnt : MapEnt
        {
            public PointEnt(string className, Coords origin) : base(className)
            {
                properties.Add("origin", origin.x + " " + origin.y + " " + origin.z);
            }
        }

        public class BrushEnt : MapEnt
        {
            public List<Brush> brushes;

            public BrushEnt(string className) : base(className)
            {
                brushes = new List<Brush>();
            }

            // Create a 32x32 cube at 0, 0, 0
            public void AddDebugCube()
            {
                Coords[] corners = { new Coords(-16, 16, 16), new Coords(16, 16, 16), new Coords(16, -16, 16), new Coords(-16, -16, 16), new Coords(-16, 16, -16), new Coords(16, 16, -16), new Coords(16, -16, -16), new Coords(-16, -16, -16) };
                Brush debugCube = new Brush();
                debugCube.planes.Add(new Plane(corners[1], corners[0], corners[4], "NULL", true));
                debugCube.planes.Add(new Plane(corners[2], corners[1], corners[5], "NULL", true));
                debugCube.planes.Add(new Plane(corners[3], corners[2], corners[6], "NULL", true));
                debugCube.planes.Add(new Plane(corners[0], corners[3], corners[7], "NULL", true));
                debugCube.planes.Add(new Plane(corners[0], corners[1], corners[2], "NULL", true));
                debugCube.planes.Add(new Plane(corners[6], corners[5], corners[4], "NULL", true));
                brushes.Add(debugCube);
            }

            new public string ToMapString() // Overrides base class to account for brushes needing to go in the middle
            {
                string asString = "{\r\n";
                foreach (KeyValuePair<string, string> entry in properties)
                {
                    asString += "\t\"" + entry.Key + "\" \"" + entry.Value + "\"\r\n";
                }
                foreach(Brush brush in brushes)
                {
                    asString += brush.ToMapString();
                }
                asString += "}\r\n";
                return asString;
            }
        }

        public class Brush
        {
            

            public enum InitialShape { BLOCK, TETRAHEDRON, PYRAMID, WEDGE, CYLINDER, CONE } // Used purely as a shortcut to tell the constructor which planes to initially define; the brush does NOT necessarily retain this shape over the course of its lifetime

            public HashSet<Plane> planes;

            // Defining an empty brush *really* shouldn't be done unless you need some really funky shape
            // and you plan to IMMEDIATELY add the planes to define it
            public Brush()
            {
                planes = new HashSet<Plane>();
            }


            // Coordinate pairs are upper top left and lower bottom right bounds. 
            // 'up' is a vector (must be cartesian) that represents the direction of the 'top' of certain oriented shapes (end cap of cylinders, point of pyramids, etc)
            // numSides is for shapes that must approximate a curve or circular face (cones & cylinders), is ignored for other shapes
            public Brush(InitialShape shape, string texName, Coords p1, Coords p2, bool alignToWorld, Vector up, int numSides) : this()
            {
                switch (shape)
                {
                    // Probably gonna want to break this up into a bunch of internal methods at some point, yeah?
                    case InitialShape.BLOCK:
                        Coords[] blockCorners = { new Coords(p1), new Coords(p2.x, p1.y, p1.z), new Coords(p2.x, p2.y, p1.z), new Coords(p1.x, p2.y, p1.z), new Coords(p1.x, p1.y, p2.z), new Coords(p2.x, p1.y, p2.z), new Coords(p2), new Coords(p1.x, p2.y, p2.z) };

                        planes.Add(new Plane(blockCorners[1], blockCorners[0], blockCorners[4], texName, alignToWorld));
                        planes.Add(new Plane(blockCorners[2], blockCorners[1], blockCorners[5], texName, alignToWorld));
                        planes.Add(new Plane(blockCorners[3], blockCorners[2], blockCorners[6], texName, alignToWorld));
                        planes.Add(new Plane(blockCorners[0], blockCorners[3], blockCorners[7], texName, alignToWorld));
                        planes.Add(new Plane(blockCorners[0], blockCorners[1], blockCorners[2], texName, alignToWorld));
                        planes.Add(new Plane(blockCorners[6], blockCorners[5], blockCorners[4], texName, alignToWorld));
                        break;
                    case InitialShape.TETRAHEDRON:
                        Coords[] tetraCorners = { new Coords(p1), new Coords(p1.x, p1.y, p2.z), new Coords(p2.x, p1.y, p2.z), new Coords(p1.x, p2.y, p2.z)};

                        planes.Add(new Plane(tetraCorners[2], tetraCorners[1], tetraCorners[3], texName, alignToWorld));
                        planes.Add(new Plane(tetraCorners[0], tetraCorners[2], tetraCorners[3], texName, alignToWorld));
                        planes.Add(new Plane(tetraCorners[0], tetraCorners[3], tetraCorners[1], texName, alignToWorld));
                        planes.Add(new Plane(tetraCorners[0], tetraCorners[1], tetraCorners[2], texName, alignToWorld));
                        break;
                    case InitialShape.PYRAMID:
                        Coords[] pmidCorners = { new Coords(p1.x + (p2.x - p1.x) / 2, p1.y - (p1.y - p2.y) / 2, p1.z), new Coords(p1.x, p1.y, p2.z), new Coords(p2.x, p1.y, p2.z), new Coords(p2), new Coords(p1.x, p2.y, p2.z) };

                        planes.Add(new Plane(pmidCorners[3], pmidCorners[2], pmidCorners[1], texName, alignToWorld));
                        planes.Add(new Plane(pmidCorners[0], pmidCorners[1], pmidCorners[2], texName, alignToWorld));
                        planes.Add(new Plane(pmidCorners[0], pmidCorners[2], pmidCorners[3], texName, alignToWorld));
                        planes.Add(new Plane(pmidCorners[0], pmidCorners[3], pmidCorners[4], texName, alignToWorld));
                        planes.Add(new Plane(pmidCorners[0], pmidCorners[4], pmidCorners[1], texName, alignToWorld));
                        break;
                    case InitialShape.WEDGE:
                        Coords[] wedgeCorners = { new Coords(p1.x + (p2.x - p1.x) / 2, p1.y, p1.z), new Coords(p1.x + (p2.x - p1.x) / 2, p2.y, p1.z), new Coords(p1.x, p1.y, p2.z), new Coords(p2.x, p1.y, p2.z), new Coords(p2), new Coords(p1.x, p2.y, p2.z) };

                        planes.Add(new Plane(wedgeCorners[4], wedgeCorners[3], wedgeCorners[2], texName, alignToWorld));
                        planes.Add(new Plane(wedgeCorners[0], wedgeCorners[1], wedgeCorners[5], texName, alignToWorld));
                        planes.Add(new Plane(wedgeCorners[1], wedgeCorners[0], wedgeCorners[3], texName, alignToWorld));
                        planes.Add(new Plane(wedgeCorners[5], wedgeCorners[1], wedgeCorners[4], texName, alignToWorld));
                        planes.Add(new Plane(wedgeCorners[3], wedgeCorners[0], wedgeCorners[2], texName, alignToWorld));
                        break;
                    case InitialShape.CYLINDER:
                        // god, this is going to destroy me
                        // okay, so we need to define the end cap
                        Coords[] cylinderTopCap = new Coords[numSides];
                        int cylinderA = (p2.x - p1.x) / 2;
                        int cylinderB = (p2.y - p1.y) / 2;
                        for (int i = 0; i < numSides; i++)
                        {
                            // using parametric representation to approximate points (we round our vertices to the 1-unit grid, obviously)
                            int x = (int)(Math.Round(cylinderA * Math.Cos((2 * Math.PI / numSides) * i)));
                            int y = (int)(Math.Round(cylinderB * Math.Sin((2 * Math.PI / numSides) * i)));
                            cylinderTopCap[i] = new Coords(p1.x + cylinderA + x, p1.y + cylinderB + y, p1.z);
                        }
                        for (int i = 1; i < numSides; i++)
                        {
                            // Go around the sides and iteratively fill in planes
                            Coords lowerEquivalent = new Coords(cylinderTopCap[i].x, cylinderTopCap[i].y, p2.z);
                            planes.Add(new Plane(lowerEquivalent, cylinderTopCap[i], cylinderTopCap[i - 1], texName, alignToWorld));
                        }
                        // Final side needs to go a little differently, oops
                        planes.Add(new Plane(new Coords(cylinderTopCap[0].x, cylinderTopCap[0].y, p2.z), cylinderTopCap[0], cylinderTopCap[numSides - 1], texName, alignToWorld));

                        // Okay, so, end caps:
                        // Because we're rounding vertices, it's possible we'll create sets of collinear points. 
                        // This isn't a *HUGE* problem (because vertices aren't actually real) but it means we can't just pick the first three clockwise points of the end cap.
                        // Instead, we choose the first, middle, and last
                        // In the worst possible case (a 'cylinder' with only three points) this should still work
                        planes.Add(new Plane(cylinderTopCap[0], cylinderTopCap[(numSides - 1) / 2], cylinderTopCap[numSides - 1], texName, alignToWorld));
                        planes.Add(new Plane(new Coords(cylinderTopCap[numSides - 1].x, cylinderTopCap[numSides - 1].y, p2.z), new Coords(cylinderTopCap[(numSides - 1) / 2].x, cylinderTopCap[(numSides - 1) / 2].y, p2.z), new Coords(cylinderTopCap[0].x, cylinderTopCap[0].y, p2.z), texName, alignToWorld));
                        break;
                    case InitialShape.CONE:
                        // Similar to the cylinder code, for obvious reasons
                        Coords[] coneCap = new Coords[numSides];  
                        int coneA = (p2.x - p1.x) / 2;
                        int coneB = (p2.y - p1.y) / 2;
                        Coords conePoint = new Coords(p1.x + coneA, p1.y + coneB, p1.z);
                        for (int i = 0; i < numSides; i++)
                        {
                            // using parametric representation to approximate points (we round our vertices to the 1-unit grid, obviously)
                            int x = (int)(Math.Round(coneA * Math.Cos((2 * Math.PI / numSides) * i)));
                            int y = (int)(Math.Round(coneB * Math.Sin((2 * Math.PI / numSides) * i)));
                            coneCap[i] = new Coords(p1.x + coneA + x, p1.y + coneB + y, p2.z);
                        }
                        for (int i = 0; i < numSides - 1; i++)
                        {
                            planes.Add(new Plane(coneCap[i], coneCap[i + 1], conePoint, texName, alignToWorld));
                        }
                        // Again, final side needs to loop back to start
                        planes.Add(new Plane(coneCap[numSides - 1], coneCap[0], conePoint, texName, alignToWorld));

                        planes.Add(new Plane(coneCap[1], coneCap[0], coneCap[numSides - 1], texName, alignToWorld));
                        break;
                }
                // TODO: Rotate shape to point towards up vector
            }

            // Don't feel like supplying number of sides? (Or don't need it?) This'll cover you.
            public Brush(InitialShape shape, string texName, Coords p1, Coords p2, bool alignToWorld, Vector up) : this(shape, texName, p1, p2, alignToWorld, up, 16)
            {

            }

            // Default vector is Z-up, since we're probably going to be working with floor plans a lot
            public Brush(InitialShape shape, string texName, Coords p1, Coords p2, bool alignToWorld) : this(shape, texName, p1, p2, alignToWorld, new Vector(0, 0, 1))
            {

            }

            public Brush(InitialShape shape, string texName, Coords p1, Coords p2) : this(shape, texName, p1, p2, true)
            {

            }

            // Test if two rectangular prism brushes share a side. If they do, returns the corresponding planes along which they touch.
            // Returns (null, null) otherwise.
            // This function is a simplified version of the problem because it turns out generalising it to work for any kind of convex shape
            // is a GODDAMN NIGHTMARE OH MY GOD
            public static (Plane, Plane) TestRectAdjacency(Brush b1, Brush b2)
            {
                // Two conditions must be satisfied in order for brushes to be touching:
                // We must be able to find a pair of planes with opposite normals and zero distance between them
                // (i.e. touching planes)
                // AND
                // We must *NOT* be able to find a pair of planes with opposite normals and *positive* distance between them
                // (i.e. the planes are parallel and on the outside of each other)
                Plane side1 = null;
                Plane side2 = null;
                bool gap = false;
                foreach (Plane pl1 in b1.planes)
                {
                    foreach (Plane pl2 in b2.planes)
                    {
                        if (pl1.Normal().Equals(pl2.InverseNormal()))
                        {
                            // 'diff' is a line between arbitrary points on each plane.
                            // Thus, if the planes are touching, that line will be perpendicular to their normals
                            // (There shouldn't be more than one touching pair, unless somebody's been fucking around with vertices)
                            // And if the planes have a positive distance between them, the line will form an acute angle with their normals
                            Vector diff = new Vector(pl1.p1.x - pl2.p1.x, pl1.p1.y - pl2.p1.y, pl1.p1.z - pl2.p1.z);
                            if (Vector.DotProduct(diff, pl2.Normal()) == 0)
                            {
                                side1 = pl1;
                                side2 = pl2;
                            }
                            if (Vector.DotProduct(diff, pl2.Normal()) > 0)
                            {
                                gap = true;
                                side1 = null;
                                side2 = null;
                                break;
                            }
                        }
                    }
                    if (gap)
                    {
                        break;
                    }
                }
                return (side1, side2);
            }

            public string ToMapString() // As opposed to ToString, this supplies a properly formatted representation of the brush that can be written to the .map file (within the context of the BrushEntity, of course)
            {
                string asString = "\t{\r\n";
                foreach (Plane plane in planes)
                {
                    asString += "\t\t" + plane.p1.ToMapString() + " " + plane.p2.ToMapString() + " " + plane.p3.ToMapString() + " " + plane.texName + " [ " + plane.texRight.ToMapString() + " " + plane.xOffset + " ] [ " + plane.texDown.ToMapString() + " " + plane.yOffset + " ] " + plane.rotation + " " + plane.scaleX + " " + plane.scaleY + "\r\n";
                }
                asString += "\t}\r\n";
                return asString;
            }
        }

        public class BrushworkGenerator
        {
            public const double PATH_WIGGLYNESS = 0.5; // Degree to which paths created by the floor generator deviate from as-the-crow-flies (I guess?)
            public const int AVG_PATCH_SIDE = 512; // Average length of the side of a 'patch' (rectangular section used to generate floor plan)
            public const int PATCH_SIDE_VARIATION = 256; // Max variation in patch side length
            public List<Brush> floorBrushwork;
            public AbstractLayout layout;
            public struct GeneratorParams
            {
                
            }

            public BrushworkGenerator()
            {
                floorBrushwork = new List<Brush>();
                layout = new AbstractLayout(false);
            }

            public BrushworkGenerator(AbstractLayout layout) : this()
            {
                this.layout = layout;
            }

            // Makes a patch of random size guaranteed to contain the supplied origin
            public Brush MakePatch(Coords origin, int avgSideLength, int sideVariation, Random rng)
            {
                int xLen = avgSideLength + rng.Next(-sideVariation, sideVariation);
                int yLen = avgSideLength + rng.Next(-sideVariation, sideVariation);
                int xOffset = rng.Next(xLen);
                int yOffset = rng.Next(yLen);

                Coords p1 = new Coords(origin.x - xOffset, origin.y + yOffset, origin.z);
                Coords p2 = new Coords(p1.x + xLen, p1.y - yLen, p1.z - 32);

                return new Brush(Brush.InitialShape.BLOCK, "NULL", p1, p2);
            }

            // Grow a patch outward from an existing brush in such a way that it doesn't intersect with existing floor patches.
            // Returns the brush itself and also a point indicating the middle of the brush
            public (Brush, Coords) BudPatch(Brush parent, Vector bearing, int avgSideLength, int sideVariation, Random rng)
            {
                Brush bud = null;
                int targetHeight = avgSideLength + rng.Next(-sideVariation, sideVariation);
                int targetWidth = avgSideLength + rng.Next(-sideVariation, sideVariation);
                int floorHeight = -9999;
                // There's got to be a quicker way to pull the right plane out of the set, right???
                foreach(Plane plane in parent.planes)
                {
                    if (plane.Normal().z > 0)
                    {
                        floorHeight = plane.p1.z;
                        break;
                    }
                }
                // It's easiest to think of this problem in terms of a top-down, two-dimensional format, I think
                // Sorry if I mix and match language a bit.
                bool readyToBud = false;
                Plane parentPlane = null;
                while (!readyToBud)
                {
                    // First step: find a plane to bud from by seeing which normal matches our bearing the closest
                    double smallestAngle = 360;
                    foreach (Plane pl in parent.planes)
                    {
                        double diff = Vector.AngleBetween(pl.Normal(), bearing);
                        if (diff < smallestAngle)
                        {
                            smallestAngle = diff;
                            parentPlane = pl;
                        }
                    }
                    // Now check: is there a brush already touching this side?
                    // If so, we move to that brush and try again.
                    bool foundAdjacency = false;
                    foreach (Brush touchingBrush in this.floorBrushwork)
                    {
                        if (touchingBrush != parent)
                        {
                            (Plane pl1, Plane pl2) = Brush.TestRectAdjacency(parent, touchingBrush);
                            if (pl1 == parentPlane)
                            {
                                parent = touchingBrush;
                                foundAdjacency = true;
                                break;
                            }
                        }
                    }
                    // If no adjacency was found on the selected surface, we can start to grow our new brush.
                    // Otherwise, the process repeats with a new 'parent'
                    if (!foundAdjacency)
                    {
                        readyToBud = true;
                    }
                }
                // Select a point on the valid surface to bud from.
                // Height is immaterial, so this can be thought of a point somewhere on a continuum between the two corners.
                // (which we need to get first, obviously)
                Vector dummy;
                Coords startCorner = new Coords();
                Coords endCorner = new Coords();
                foreach (Plane pl in parent.planes)
                {
                    if (pl.Normal().z == 0 && pl.Normal().BasicallyEquals(Vector.RotateAboutAxis(parentPlane.Normal(), Vector.Up(), Math.PI / 2), 0.2))
                    {
                        (dummy, startCorner) = Plane.Intersection(pl, parentPlane);
                        startCorner.z = floorHeight;
                    }
                    if (pl.Normal().z == 0 && pl.Normal().BasicallyEquals(Vector.RotateAboutAxis(parentPlane.Normal(), Vector.Up(), 3 * Math.PI / 2), 0.2))
                    {
                        (dummy, endCorner) = Plane.Intersection(pl, parentPlane);
                        endCorner.z = floorHeight;
                    }
                }
                Vector line = new Vector(startCorner, endCorner);
                double scale = rng.NextDouble();
                Coords seedPoint = new Coords(startCorner.x + (int)Math.Round(line.x * scale), startCorner.y + (int)Math.Round(line.y * scale), startCorner.z - 16);

                // Grow a line perpendicular to parent plane at the valid point, to determine 'height' of the rect
                Vector.TraceInfo initialTrace = Vector.Trace(seedPoint, parentPlane.Normal(), this.floorBrushwork, targetHeight);

                // Choose a direction and grow a rectangle with the line's height
                int sign = (rng.Next(0, 2) * 2) - 1;
                Vector direction = Vector.RotateAboutAxis(parentPlane.Normal(), Vector.Up(), Math.PI / 2) * sign;
                int maxWidth = targetWidth;
                for (int i = 0; i < initialTrace.traceLen; i++)
                {
                    Coords widthTraceStart = new Coords((int)Math.Round(seedPoint.x + parentPlane.Normal().x * i), (int)Math.Round(seedPoint.y + parentPlane.Normal().y * i), (int)Math.Round(seedPoint.z + parentPlane.Normal().z * i));
                    Vector.TraceInfo widthTrace = Vector.Trace(widthTraceStart, direction, this.floorBrushwork, maxWidth);
                    if (widthTrace.traceLen < maxWidth)
                    {
                        maxWidth = (int)Math.Round(widthTrace.traceLen);
                    }
                }
                // Right, we have enough info to actually construct the brush
                // Now we just need to sort it out
                // (There's probably a more efficient way to do this, but w/e, it's four points, gimme a break)
                Coords[] corners = new Coords[4];
                corners[0] = seedPoint;
                corners[1] = initialTrace.finishPoint;
                corners[2] = seedPoint + direction * maxWidth;
                corners[3] = initialTrace.finishPoint + direction * maxWidth;
                int maxX = -9999;
                int maxY = -9999;
                int minX = 9999;
                int minY = 9999;
                foreach (Coords corner in corners)
                {
                    if (corner.x < minX)
                    {
                        minX = corner.x;
                    }
                    if (corner.x > maxX)
                    {
                        maxX = corner.x;
                    }
                    if (corner.y < minY)
                    {
                        minY = corner.y;
                    }
                    if (corner.y > maxY)
                    {
                        maxY = corner.y;
                    }
                }

                Coords topLeft = new Coords(minX, maxY, floorHeight);
                Coords bottomRight = new Coords(maxX, minY, floorHeight - 32);
                bud = new Brush(Brush.InitialShape.BLOCK, "null", topLeft, bottomRight);
                Coords budCentre = new Coords(minX + ((maxX - minX) / 2), minY + ((maxY - minY) / 2), floorHeight);
                Console.WriteLine("Generated brush between " + topLeft.ToMapString() + " and " + bottomRight.ToMapString());
                return (bud, budCentre);
            }

            public void MakeFloor()
            {
                Random rng = new Random();
                // First step: generate patches around the 'locations' defined by the AbstractLayout, to ensure they're actually reachable
                
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (layout.locales[i, j].purpose != LocationPurpose.INACCESSIBLE)
                        {
                            floorBrushwork.Add(MakePatch(layout.locales[i, j].coords, AVG_PATCH_SIDE, PATCH_SIDE_VARIATION, rng)); // Use this one for more positioning variability
                        }
                    }
                }
                // Generate paths between locations
                foreach (Path path in layout.paths)
                {
                    Location[] ends = path.GetLocations();
                    Location source = ends[0];
                    Location dest = ends[1];
                    // We build paths from the highest point to the lowest point, so make sure we start from the one with the higher Z
                    if (ends[1].coords.z > ends[0].coords.z)
                    {
                        source = ends[1];
                        dest = ends[0];
                    }
                    MakePath(source, dest, PATH_WIGGLYNESS, AVG_PATCH_SIDE, PATCH_SIDE_VARIATION, rng, false);
                    Console.WriteLine("Generated path between " + path.GetLocations()[0].coords.ToMapString() + " and " + path.GetLocations()[1].coords.ToMapString());

                }

                
            }

            public void MakePath(Location source, Location dest, double wiggliness, int avgSideLength, int sideVariation, Random rng, bool useUniform)
            {
                Coords currentPos = source.coords;
                Brush currentBrush;
                Vector nextBearing; 
                bool destReached = false;
                // Is a brush already at the start? If not, make one and make it the currentBrush
                Vector.TraceInfo sourceTrace = Vector.Trace(new Coords(source.coords.x, source.coords.y, source.coords.z - 16), new Vector(0, 0, 1), floorBrushwork, Vector.MAX_TRACE_LENGTH);
                if (sourceTrace.hit)
                {
                    currentBrush = sourceTrace.impactBrush;
                }
                else
                {
                    currentBrush = MakePatch(source.coords, AVG_PATCH_SIDE, PATCH_SIDE_VARIATION, rng);
                    floorBrushwork.Add(currentBrush);
                }

                while (!destReached)
                {
                    // Initial ('ideal') bearing is straight towards the destination
                    nextBearing = Vector.Normalise(new Vector(currentPos, dest.coords));
                    // The bearing can be rotated up to 120 degrees, according to a distribution influenced by wiggliness and useUniform
                    double z;
                    if (useUniform)
                    {
                        z = rng.Next(-120, 120) * wiggliness;
                        nextBearing = Vector.RotateAboutAxis(nextBearing, new Vector(0, 0, 1), z * Math.PI / 180);
                    }
                    else
                    {
                        // We use Box-Muller to transform the uniform distribution into a standard normal distribution (mean = 0, sd = 1)
                        z = Math.Sqrt(-2 * Math.Log(rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());
                        z *= wiggliness * 120;
                    }
                    nextBearing = Vector.RotateAboutAxis(nextBearing, new Vector(0, 0, 1), z * Math.PI / 180);
                    // Bud out and update our 'position' (roughly) to the new brush
                    Brush nextBrush;
                    (nextBrush, currentPos) = BudPatch(currentBrush, nextBearing, AVG_PATCH_SIDE, PATCH_SIDE_VARIATION, rng);
                    floorBrushwork.Add(nextBrush);
                    currentBrush = nextBrush;
                    // How do we know if we reached the destination? Either our currentBrush covers it, OR, it shares a side with a brush that already does
                    Vector.TraceInfo destTrace = Vector.Trace(new Coords(dest.coords.x, dest.coords.y, dest.coords.z - 16), new Vector(0, 0, 1), floorBrushwork, Vector.MAX_TRACE_LENGTH);
                    if (destTrace.hit)
                    {
                        if (destTrace.impactBrush == currentBrush)
                        {
                            destReached = true;
                        }
                        else
                        {
                            (Plane p1, Plane p2) = Brush.TestRectAdjacency(currentBrush, destTrace.impactBrush);
                            if (destTrace.hit && p1 != null && p2 != null)
                            {
                                destReached = true;
                            }
                        }
                    }
                }
            }

        }

        const string defaultPath = "D:\\Games\\Steam\\steamapps\\common\\Half-Life SDK\\Map Files\\test.map";
        private StreamWriter file;
        public BrushEnt worldspawn; // worldspawn is technically just another entity, but because it houses global properties and all non-entity brushes, it probably makes sense to keep it separate.
        public List<MapEnt> mapEnts;
        public BrushworkGenerator generator;

        public MapFile(string filePath)
        {
            try
            {
                file = new StreamWriter(filePath, false); // Choosing a default is ehhhh, but as we're working on a generator, it's likely we want to just overwrite everything
                mapEnts = new List<MapEnt>();
                worldspawn = new BrushEnt("worldspawn");
                generator = new BrushworkGenerator();

                // Otherwise known as map properties. Contains various global properties and also some basic stuff that every map needs 
                worldspawn.properties.Add("spawnflags", "0"); // Worldspawn doesn't have spawnflags, but most maps seem to have this property in them anyway
                worldspawn.properties.Add("mapversion", "220"); // Default for GoldSrc/HL
                worldspawn.properties.Add("light", "0"); // Default (i.e. minimum) light level
                worldspawn.properties.Add("MaxRange", "4096"); // Far clipping plane. Can probably go pretty nuts with it these days
                worldspawn.properties.Add("wad", ""+ // Included wads. Paths are absolute and semicolon-separated. This is probably gonna be overwritten at compile time, but here are some defaults
                    "D:\\Games\\Steam\\steamapps\\common\\Half-Life\\valve\\liquids.wad;"+
                    "D:\\Games\\Steam\\steamapps\\common\\Half-Life\\valve\\xeno.wad;"+
                    "D:\\Games\\Steam\\steamapps\\common\\Half-Life\\valve\\zhlt.wad;"+
                    "D:\\Games\\Steam\\steamapps\\common\\Half-Life\\valve\\halflife.wad");
                
            }
            catch (FileLoadException e)
            {
                Console.WriteLine("Could not load file.");
                Console.WriteLine(e.Message);
                Logger.log(e.Message);
            }
            catch (IOException e)
            {
                Console.WriteLine("Unspecified IO error.");
                Console.WriteLine(e.Message);
                Logger.log(e.Message);
            }
            
        }

        // Chooses a path for you, if you're that dang lazy
        public MapFile() : this(defaultPath)
        {
            // Uh. I'm not sure if I need to put anything here
        }

        // Writes contents of map (worldspawn + mapEnts) to file.
        public void SaveMap()
        {
            try
            {
                file.Write(worldspawn.ToMapString());
                foreach (MapEnt ent in mapEnts)
                {
                    file.Write(ent.ToMapString());
                }
                file.Flush();
            }
            catch (IOException e)
            {
                Console.WriteLine("IO error.");
                Console.WriteLine(e.Message);
                Logger.log(e.Message);
            }
        }

        // Debug function that draws the supplied abstract AbstractLayout into the map
        public void DrawAbstractLayout(AbstractLayout layout)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (layout.locales[i, j].purpose != LocationPurpose.INACCESSIBLE)
                    {
                        Coords origin = layout.locales[i, j].coords;
                        PointEnt locationMarker = new PointEnt("info_null", origin); // info_null is a dummy entity designed for spotlights to point at, so it's useful as a non-entity
                        locationMarker.properties.Add("targetname", "DEBUG: [" + i + ", " + j + "]");
                        mapEnts.Add(locationMarker);
                    }
                }
            }
            // GoldSrc editors don't appear to be capable of visualising paths, so we have to do this with brushwork :(
            foreach (Path path in layout.paths)
            {
                Location[] locations = path.GetLocations();
                Brush pathBrush = new Brush(Brush.InitialShape.BLOCK, "NULL", new Coords(-4096, 4096, -4032), new Coords(4096, -4096, -4096));
                Vector pathLine = new Vector(locations[1].coords.x - locations[0].coords.x, locations[1].coords.y - locations[0].coords.y, 0); // Create a vector from location[0] to location[1]
                Vector perpCW = Vector.Normalise(new Vector(pathLine.y * -1, pathLine.x, 0));
                Vector perpCCW = Vector.Normalise(new Vector(pathLine.y, pathLine.x * -1, 0));

                Coords p1 = new Coords((int)(locations[0].coords.x + perpCW.x * 8), (int)(locations[0].coords.y + perpCW.y * 8), 0);
                Coords p2 = new Coords((int)(p1.x + pathLine.x), (int)(p1.y + pathLine.y), 0);
                Coords p3 = new Coords(p1.x, p1.y, 1024);
                pathBrush.planes.Add(new Plane(p3, p1, p2, "NULL", true));

                p1 = new Coords((int)(locations[0].coords.x + perpCCW.x * 8), (int)(locations[0].coords.y + perpCCW.y * 8), 0);
                p2 = new Coords((int)(p1.x + pathLine.x), (int)(p1.y + pathLine.y), 0);
                p3 = new Coords(p1.x, p1.y, 1024);
                pathBrush.planes.Add(new Plane(p2, p1, p3, "NULL", true));

                
                p1 = new Coords(locations[0].coords);
                p2 = new Coords((int)(p1.x + perpCW.x * 8), (int)(p1.y + perpCW.y * 8), 0);
                p3 = new Coords(p1.x, p1.y, 1024);
                pathBrush.planes.Add(new Plane(p3, p1, p2, "NULL", true));

                p1 = new Coords(locations[1].coords);
                p2 = new Coords((int)(p1.x + perpCCW.x * 8), (int)(p1.y + perpCCW.y * 8), 0);
                p3 = new Coords(p1.x, p1.y, 1024);
                pathBrush.planes.Add(new Plane(p3, p1, p2, "NULL", true));
                
                worldspawn.brushes.Add(pathBrush);
            }
        }  
    }

    class Program
    {
        static void Main(string[] args)
        {
            AbstractLayout test = new AbstractLayout(false);
            test.GenerateSpatialCoords();

            MapFile map = new MapFile();
            map.DrawAbstractLayout(test);
            map.generator = new MapFile.BrushworkGenerator(test);
            /*for (int i = 1; i <= 16; i++)
            {
                Coords topLeft = new Coords(i * 256, 0, 256);
                Coords bottomRight = new Coords(topLeft.x + i * 16, i * -16, 256 - (i * 16));
                //map.worldspawn.brushes.Add(new MapFile.Brush(MapFile.Brush.InitialShape.CONE, "NULL", topLeft, bottomRight, false, new Vector(0, 0, 1), 16));
            }*/
            map.generator.MakeFloor();
            map.worldspawn.brushes.AddRange(map.generator.floorBrushwork);

            map.SaveMap();
        }

        
    }
}
