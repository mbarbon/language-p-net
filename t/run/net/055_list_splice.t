#!/usr/bin/perl -w

print "1..10\n";

if( defined &Internals::Net::get_class ) {
    $date_class = Internals::Net::get_class( 'System.DateTime' );

    $generic_list = Internals::Net::get_class( 'System.Collections.Generic.List`1' );
    $date_list = Internals::Net::specialize_type( $generic_list, $date_class );

    $array = Internals::Net::create( $date_list );
    $date1 = Internals::Net::create( $date_class, 2010, 12, 16, 23, 13, 42 );
    $date2 = Internals::Net::create( $date_class, 2010, 12, 16, 23, 13, 43 );
    $date3 = Internals::Net::create( $date_class, 2010, 12, 16, 23, 13, 44 );

    push @$array, $date1, $date2, $date1, $date3;
    @val = splice @$array, 1, 2;
    print @$array == 2 ? "ok\n" : "not ok\n";
    print @val == 2 ? "ok\n" : "not ok\n";
    print $array->[0] eq $date1 ? "ok\n" : "not ok\n";
    print $array->[1] eq $date3 ? "ok\n" : "not ok\n";
    print $val[0] eq $date2 ? "ok\n" : "not ok\n";
    print $val[1] eq $date1 ? "ok\n" : "not ok\n";

    splice @$array, 0, 1, @val;
    print @$array == 3 ? "ok\n" : "not ok\n";
    print $array->[0] eq $date2 ? "ok\n" : "not ok\n";
    print $array->[1] eq $date1 ? "ok\n" : "not ok\n";
    print $array->[2] eq $date3 ? "ok\n" : "not ok\n";
} else {
    print "ok - skipped\n" for 1..10;
}
