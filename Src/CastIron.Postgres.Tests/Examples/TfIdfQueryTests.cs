using System.Collections.Generic;
using System.Linq;
using System.Text;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Examples
{
    [TestFixture]
    public class TfIdfQueryTests
    {
        private class CreateTfIdfTableCommand : ISqlCommandSimple
        {
            public string GetSql()
            {
                return @"
CREATE TEMP TABLE DocumentTerms (
    Id SERIAL PRIMARY KEY,
    DocumentId INT NOT NULL,
    Term VARCHAR(32) NOT NULL,
    Occurances INT NOT NULL
);";
            }
        }

        private class PopulateTermsTableCommand : ISqlCommandSimple
        {
            private readonly IReadOnlyList<string> _documents;

            public PopulateTermsTableCommand(IEnumerable<string> documents)
            {
                _documents = documents.ToList();
            }

            public string GetSql()
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _documents.Count; i++)
                {
                    var document = _documents[i];
                    var terms = document.Split(" ").GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
                    foreach (var termCounts in terms)
                        sb.AppendLine($"INSERT INTO DocumentTerms (DocumentId, Term, Occurances) VALUES ({i + 1}, '{termCounts.Key}', {termCounts.Value});");
                }

                return sb.ToString();
            }
        }

        private class TfIdfResult
        {
            public int DocumentId { get; set; }
            public float TfIdfScore { get; set; }
        }

        private class TfIdfCalculationQuery : ISqlQuery<IReadOnlyList<TfIdfResult>>
        {
            private readonly string _term;
            private readonly int _start;
            private readonly int _pageSize;

            public TfIdfCalculationQuery(string term, int start = 0, int pageSize = 100)
            {
                _term = term;
                _start = start;
                _pageSize = pageSize;
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText(GetSql());
                command.AddParameterWithValue("@term", _term);
                command.AddParameterWithValue("@start", _start);
                command.AddParameterWithValue("@pageSize", _pageSize);

                return true;
            }

            public IReadOnlyList<TfIdfResult> Read(IDataResults result)
            {
                return result.AsEnumerable<TfIdfResult>().ToList();
            }

            private string GetSql()
            {
                return @"
DECLARE numberOfDocuments INT;
SELECT numberOfDocuments = COUNT(DISTINCT DocumentId) FROM DocumentTerms;

WITH
DocumentTotalOccurances AS (
    SELECT
        DocumentId,
        SUM(Occurances) AS TotalOccurances
        FROM
            #DocumentTerms
        GROUP BY
            DocumentId
),
TermFrequency AS (
    SELECT
        dt.DocumentId,
        dt.Term,
        dt.Occurances,
        dto.TotalOccurances,
        CAST(dt.Occurances AS FLOAT) / CAST(dto.TotalOccurances AS FLOAT) AS Value
        FROM
            DocumentTerms dt
            INNER JOIN
            DocumentTotalOccurances dto
                ON dt.DocumentId = dto.DocumentId
        WHERE
            Term = @term
),
InverseDocumentFrequency AS (
    SELECT
        Term,
        @numberOfDocuments AS NumberOfDocuments,
        COUNT(0) AS DocumentsWithThisTerm,
        LOG10(CAST(numberOfDocuments AS FLOAT) / CAST(COUNT(0) AS FLOAT)) AS Value
        FROM
            DocumentTerms
        WHERE
            Term = @term
        GROUP BY
            Term
),
TfIdfScores AS (
    SELECT
        tf.DocumentId,
        tf.Term,
        tf.Occurances,
        tf.TotalOccurances,
        tf.Value AS Tf,
        idf.NumberOfDocuments,
        idf.DocumentsWithThisTerm,
        idf.Value AS Idf,
        tf.Value * idf.Value AS TfIdfScore
        FROM
            TermFrequency as tf
            INNER JOIN
            InverseDocumentFrequency idf
                ON tf.Term = idf.Term
)
SELECT
    DocumentId,
    TfIdfScore
    FROM
        TfIdfScores
    WHERE
        TfIdfScore > 0
    ORDER BY
        TfIdfScore DESC OFFSET @start ROWS FETCH NEXT @pageSize ROWS ONLY;";
            }
        }

        // TODO SQLite doesn't use the same syntax for temp tables.
        [Test]
        public void TfIdf_Test()
        {
            // TODO: this
            Assert.Inconclusive("Postgres is giving syntax errors");
            var runner = RunnerFactory.Create();
            var batch = runner.CreateBatch();
            batch.Add(new CreateTfIdfTableCommand());
            batch.Add(new PopulateTermsTableCommand(new[]
            {
                // example from https://en.wikipedia.org/w/index.php?title=Tf%E2%80%93idf&oldid=848568278
                "this is a a sample",
                "this is another another example example example"
            }));
            var promise1 = batch.Add(new TfIdfCalculationQuery("this"));
            var promise2 = batch.Add(new TfIdfCalculationQuery("example"));
            
            runner.Execute(batch);

            // "this" is common and has score of 0 for all documents
            promise1.GetValue().Should().BeEmpty();

            var result = promise2.GetValue();
            result.Count.Should().Be(1);
            result[0].DocumentId.Should().Be(2);
            result[0].TfIdfScore.Should().BeInRange(0.128f, 0.132f);
        }
    }
}