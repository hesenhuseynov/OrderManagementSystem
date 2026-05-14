namespace OrderManagementSystem.Middleware
{
    public static  class GlobalExceptionMiddlewareExtensions
    {
        public static   IApplicationBuilder UseGlobalExcpetionMiddleware( this  IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExcpetionMiddleware>(); 
        }
    }
}
