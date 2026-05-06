// test/helpers/assertHelper.js

/**
 * Normalize an array of strings to lowercase
 */
function toLowerArray(arr = []) {
  return arr.map(v =>
    typeof v === 'string' ? v.toLowerCase() : v
  );
}

/**
 * Case-insensitive version of assert.includeMembers
 */
function assertIncludeMembersIgnoreCase(actual, expected, message) {
  assert.includeMembers(
    toLowerArray(actual),
    toLowerArray(expected),
    message
  );
}

/**
 * assert expected array result
 */
function expectArrayResult(actualArray, expectedArray, message = 'Array mismatch') {
  expect(actualArray, message)
    .to.be.an('array')
    .that.has.members(expectedArray);

  // Optional: ensure no extra elements
  expect(actualArray.length, `${message} (length mismatch)`)
    .to.equal(expectedArray.length);
}

module.exports = {
  toLowerArray,
  assertIncludeMembersIgnoreCase,
  expectArrayResult
};
