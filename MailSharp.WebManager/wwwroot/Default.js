onReady(() =>
{
	document.addEventListener("click", async function (e)
	{
		if (e.target.id && typeof window[e.target.id] === "function")
		{
			e.preventDefault();
			const result = window[e.target.id].call(e, e);
			if (result instanceof Promise)
				await result;
		}
	});
});

async function getstatus()
{
	const result = await netproxyasync("/api/status/all");

	document.getElementById("status").innerText =
	' Smtp:' + result.Smtp + '\n' +
	' Imap:' + result.Imap + '\n' +
	' Pop3:' + result.Pop3 + '\n';
}