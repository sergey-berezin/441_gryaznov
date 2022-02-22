using Microsoft.EntityFrameworkCore;
using ModelLibrary;
using System.Collections.Specialized;
using System.Linq;

namespace Lab2
{
    public class DetectedObjectContext : DbContext 
    {
        public DbSet<DetectedObject>? DetectedObjects { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
            =>b.Entity<DetectedObject>().HasKey(x => x.DetectedObjectId);
        protected override void OnConfiguring(DbContextOptionsBuilder o)
            =>o.UseSqlite(@"Data Source=C:\prac\441_gryaznov\Lab2\Base.db");
        public void AddElem(DetectedObject obj)
        {
            DetectedObjects?.Add(obj);
            SaveChanges();   
        }
        public void Clear()
        {
            foreach (var elem in DetectedObjects)
            {
                DetectedObjects.Remove(elem);
            }
            SaveChanges();
            
        }

        public void Delete(int objId)
        {
            var deletedObj = DetectedObjects.Where(detectedObj => detectedObj.DetectedObjectId == objId)
                                            .FirstOrDefault();
            DetectedObjects?.Remove(deletedObj);
            SaveChanges();
        }
    }
}