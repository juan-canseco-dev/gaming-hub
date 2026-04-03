using GameHub.Abstractions.Primitives;
using MediatR;


namespace GameHub.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }