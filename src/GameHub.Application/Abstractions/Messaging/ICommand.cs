using GameHub.Domain.Abstractions;
using MediatR;


namespace GameHub.Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>, IBaseCommand { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand { }
public interface IBaseCommand { }