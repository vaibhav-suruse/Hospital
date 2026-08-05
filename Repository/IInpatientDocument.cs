using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IInpatientDocument
    {
        List<InpatientDocumentModel> GetDocuments(int ipId);

        InpatientDocumentModel? GetDocumentById(int documentId);

        int InsertDocument(InpatientDocumentModel model);

        void DeleteDocument(int documentId);
    }
}
