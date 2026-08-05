using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplicationSampleTest2.Models
{
    public class InpatientDocumentModel
    {
        public int DocumentId { get; set; }

        [Required]
        public int IP_ID { get; set; }
        [NotMapped]
        public int PatientId { get; set; }

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public string? FileType { get; set; }

        public long FileSize { get; set; }

        public int UploadedBy { get; set; }

        public DateTime UploadedDate { get; set; }

        public bool IsDeleted { get; set; }

        // Used only for upload (not stored in database)
        [NotMapped]
        public IFormFile? DocumentFile { get; set; }

    }
    public class OtherDocumentsVM
    {
        public int IpdId { get; set; }

        public int PatientId { get; set; }

        public List<InpatientDocumentModel> Documents { get; set; } = new List<InpatientDocumentModel>();

        public InpatientDocumentModel Document { get; set; } = new InpatientDocumentModel();
    }
}
