using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Security.Queries;

/// <summary>Returns cosine similarity (0-1) of active-hour distributions between two users.</summary>
public record GetActiveHoursPatternQuery(Guid UserId1, Guid UserId2) : IRequest<double>;

public class GetActiveHoursPatternQueryHandler : IRequestHandler<GetActiveHoursPatternQuery, double>
{
    private readonly IUnitOfWork _uow;
    public GetActiveHoursPatternQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<double> Handle(GetActiveHoursPatternQuery query, CancellationToken ct)
    {
        // Combine message + voice activity into 24-element hour vectors
        var msgHours1 = await _uow.Messages.GetHourlyActivityAsync(query.UserId1);
        var msgHours2 = await _uow.Messages.GetHourlyActivityAsync(query.UserId2);
        var vcHours1 = await _uow.VoiceSessions.GetHourlyActivityAsync(query.UserId1);
        var vcHours2 = await _uow.VoiceSessions.GetHourlyActivityAsync(query.UserId2);

        var vec1 = new int[24];
        var vec2 = new int[24];
        for (int i = 0; i < 24; i++)
        {
            vec1[i] = msgHours1[i] + vcHours1[i];
            vec2[i] = msgHours2[i] + vcHours2[i];
        }

        return CosineSimilarity(vec1, vec2);
    }

    private static double CosineSimilarity(int[] a, int[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < 24; i++)
        {
            dot += a[i] * b[i];
            magA += (double)a[i] * a[i];
            magB += (double)b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
