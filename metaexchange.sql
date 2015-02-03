-- phpMyAdmin SQL Dump
-- version 3.4.9
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Feb 03, 2015 at 06:37 PM
-- Server version: 5.1.61
-- PHP Version: 5.3.3

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `metaexchange`
--

-- --------------------------------------------------------

--
-- Table structure for table `exceptions`
--

CREATE TABLE IF NOT EXISTS `exceptions` (
  `uid` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `txid` varchar(64) NOT NULL,
  `message` varchar(255) NOT NULL,
  `date` datetime NOT NULL,
  `type` enum('bitcoinDeposit','bitsharesDeposit','bitcoinRefund','bitsharesRefund','none') NOT NULL,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=379 ;

-- --------------------------------------------------------

--
-- Table structure for table `general_exceptions`
--

CREATE TABLE IF NOT EXISTS `general_exceptions` (
  `hash` int(10) unsigned NOT NULL,
  `message` text COLLATE utf8_unicode_ci NOT NULL,
  `date` datetime NOT NULL,
  `count` int(11) NOT NULL DEFAULT '0',
  UNIQUE KEY `hash` (`hash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ignored`
--

CREATE TABLE IF NOT EXISTS `ignored` (
  `uid` int(11) NOT NULL AUTO_INCREMENT,
  `txid` varchar(64) NOT NULL,
  `notes` varchar(255) NOT NULL,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=16 ;

-- --------------------------------------------------------

--
-- Table structure for table `sender_to_deposit`
--

CREATE TABLE IF NOT EXISTS `sender_to_deposit` (
  `sender` varchar(64) NOT NULL,
  `deposit_address` varchar(36) NOT NULL,
  UNIQUE KEY `sender` (`sender`),
  UNIQUE KEY `deposit_address` (`deposit_address`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `stats`
--

CREATE TABLE IF NOT EXISTS `stats` (
  `last_bitshares_block` int(10) unsigned NOT NULL DEFAULT '0',
  `last_bitcoin_block` varchar(64) DEFAULT NULL,
  `uid` int(11) NOT NULL AUTO_INCREMENT,
  `bid_price` decimal(18,8) NOT NULL,
  `ask_price` decimal(18,8) NOT NULL,
  PRIMARY KEY (`uid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=3 ;

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE IF NOT EXISTS `transactions` (
  `received_txid` varchar(64) NOT NULL,
  `sent_txid` varchar(64) DEFAULT NULL,
  `asset` varchar(8) NOT NULL,
  `amount` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `date` datetime NOT NULL,
  `type` enum('bitcoinDeposit','bitsharesDeposit','bitcoinRefund','bitsharesRefund','none') NOT NULL,
  `notes` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`received_txid`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
