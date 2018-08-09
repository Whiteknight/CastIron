using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Examples
{
    [TestFixture]
    public class JaccardQueryTests
    {
        class CreateJaccardTableCommand : ISqlCommand
        {
            public string GetSql()
            {
                return @"
CREATE TABLE #Relations (
    ID INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    DocumentId INT NOT NULL,
    Term INT NOT NULL
);
CREATE UNIQUE NONCLUSTERED INDEX RelationsDocumentTerm
    ON #Relations (DocumentId, Term);";
            }
        }

        class PopulateRelationsTable : ISqlCommand
        {
            private readonly Dictionary<int, List<int>> _terms;

            public PopulateRelationsTable(Dictionary<int, List<int>> terms)
            {
                _terms = terms;
            }

            public string GetSql()
            {
                var sb = new StringBuilder();
                foreach (var document in _terms)
                {
                    var documentId = document.Key;
                    foreach (var term in document.Value)
                        sb.AppendLine($"INSERT INTO #Relations (DocumentId, Term) VALUES ({documentId},{term});");
                }

                return sb.ToString();
            }
        }

        class JaccardMatch
        {
            public int DocumentId { get; set; }
            public double Score { get; set; }
        }

        class JaccardQuery : ISqlQueryRawCommand<IReadOnlyList<JaccardMatch>>
        {
            private readonly int _documentId;
            private readonly int _start;
            private readonly int _pageSize;

            public JaccardQuery(int documentId, int start = 0, int pageSize = 100)
            {
                _documentId = documentId;
                _start = start;
                _pageSize = pageSize;
            }

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = GetSql();
                command.AddParameterWithValue("@documentId", _documentId);
                command.AddParameterWithValue("@start", _start);
                command.AddParameterWithValue("@pageSize", _pageSize);
                return true;
            }

            public IReadOnlyList<JaccardMatch> Read(SqlResultSet result)
            {
                return result.AsEnumerable<JaccardMatch>().ToList();
            }

            private string GetSql()
            {
                return @"
DECLARE @a TABLE (
    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Term INT NOT NULL
);
INSERT INTO @a (Term)
    SELECT Term from #Relations WHERE DocumentId = @documentId;

DECLARE @magnitudeA INT;
SELECT @magnitudeA = MAX(Id) FROM @a;

WITH
Intersections AS (
    SELECT
        -- Find all matching terms with A
        b.DocumentId,
        b.Term
        FROM
            #Relations a
            INNER JOIN
            #Relations b
                ON a.Term = b.Term
        WHERE
            a.DocumentId = @documentId
            AND
            b.DocumentId <> a.DocumentId
),
IntersectionMagnitudes AS (
    -- Get all intersection magnitudes
    SELECT
        DocumentId,
        CAST(COUNT(0) AS FLOAT) AS Magnitude
        FROM
            Intersections
        GROUP BY
            DocumentId
),
B AS (
    -- Get the complete set of all terms B
    SELECT 
        b.DocumentId,
        CAST(COUNT(0) AS FLOAT) AS Magnitude
        FROM
            IntersectionMagnitudes im
            INNER JOIN  
            #Relations b
                ON im.DocumentId = b.DocumentId
        GROUP BY
            b.DocumentId
),
Jaccard AS (
    -- Calculate the Jaccard score with the existing magnitudes of A, B and the intersection sets
    SELECT
        b.DocumentId,
        @magnitudeA AS MagnitudeA,
        b.Magnitude AS MagnitudeB,
        im.Magnitude AS MagnitudeIntersection,
        im.Magnitude / (@magnitudeA + b.Magnitude - im.Magnitude) AS Score
        FROM
            IntersectionMagnitudes im
            INNER JOIN
            B b
                ON b.DocumentId = im.DocumentId
)
-- Return an ordered, paged selection of the scores
SELECT
    *
    FROM
        Jaccard j
    ORDER BY
        Score DESC OFFSET @start ROWS FETCH NEXT @pageSize ROWS ONLY;";
            }
        }

        [Test]
        public void Jaccard_Test()
        {
            var runner = RunnerFactory.Create();
            var batch = new SqlBatch();
            batch.Add(new CreateJaccardTableCommand());
            batch.Add(new PopulateRelationsTable(new Dictionary<int, List<int>> {
                { 1, new List<int> { 1, 2, 3, 4, 5, 6 } },
                { 2, new List<int> { 1, 2, 3, 4, 5, 6 } },
                { 3, new List<int> { 10, 11, 12 } },
                { 4, new List<int> { 1, 2, 3, 10, 11, 12, 13 } },
                { 5, new List<int> { 1, 2, 3 } } 
            }));
            var resultPromise = batch.Add(new JaccardQuery(1));
            runner.Execute(batch);

            var result = resultPromise.GetValue().ToDictionary(jm => jm.DocumentId, jm => jm.Score);
            result.Count().Should().Be(3);
            result[2].Should().Be(1.0);
            result[5].Should().Be(0.5);
            result[4].Should().Be(0.3);
        }
    }
}
