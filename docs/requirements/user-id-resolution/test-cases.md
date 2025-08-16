# Test Cases: UserId Resolution via IoC

Link: `specifications.md`

## TC-01 Resolve from `sub` claim (default)

- Given a ClaimsPrincipal with `sub` = valid GUID
- When IUserContextProvider maps the principal
- Then Result is success with the typed UserId

## TC-02 Resolve from `oid` when `sub` absent

- Given no `sub` but `oid` = valid GUID
- Then mapping succeeds with `oid`

## TC-03 Resolve from `nameidentifier` when others absent

- Given only `nameidentifier` claim = valid GUID
- Then mapping succeeds with `nameidentifier`

## TC-04 Resolve from `appid` (daemon/service principal)

- Given no `sub`/`oid`/`nameidentifier` but `appid` = valid GUID
- Then mapping succeeds with `appid`

## TC-05 Fallback to `azp` when `appid` missing in app context

- Given no user claims and `azp` = valid GUID
- Then mapping succeeds with `azp`

## TC-06 Invalid GUID claim value

- Given `sub` present but not a GUID
- Then mapping fails with error code `user.invalid-id-format`

## TC-07 Missing all supported claims

- Given none of `sub`/`oid`/`nameidentifier`/`appid`/`azp` exist
- Then mapping fails with error code `user.missing-id-claim`

## TC-08 Domain remains decoupled

- Search domain abstractions for `System.Security.Claims`
- Expect none found

## TC-09 Error handling returns clear codes/messages

- Missing claims → `user.missing-id-claim`
- Invalid format → `user.invalid-id-format`
